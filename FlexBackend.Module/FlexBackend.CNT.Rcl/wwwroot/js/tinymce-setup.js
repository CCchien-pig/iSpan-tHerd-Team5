// tinymce-setup.js
document.addEventListener("DOMContentLoaded", () => {
    const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const placeholders = [
        "「用文字征服世界。」",
        "「寫吧，這裡是你的戰場！」",
        "「別怕，打錯字不會被扣薪水。」",
        "「字越多，力量越大。」",
        "「來，發表你的偉大言論。」",
        "「這裡可以容納你的一切靈感，除了你昨晚的爛笑話。」",
        "「不管是生旦淨末丑，跑龍套也能讓你激昂，寧願捨一頓飯也聽你唱。」",
        "「這是一首簡單的小情歌，唱著我們心頭的白鴿。」",
        "「想說好多話，可不知從何說起？」"
    ];
    const randomPlaceholder = placeholders[Math.floor(Math.random() * placeholders.length)];

    tinymce.init({
        selector: '#editor',
        height: 600,
        placeholder: randomPlaceholder,
        content_style: `
            body.mce-content-body[data-mce-placeholder]:empty::before {
                font-size: 20px !important;
                font-style: italic;
                color: #e63946;
            }`,
        menubar: true,
        statusbar: true,
        resize: true,
        plugins: 'image media emoticons link lists table code wordcount preview',
        toolbar: 'undo redo | bold italic underline | link image media emoticons | bullist numlist | table | code | wordcount | preview',
        media_live_embeds: true,
        file_picker_types: 'image media',
        images_upload_url: '/upload/image',

        images_upload_handler: async (blobInfo, progress) => {
            const form = new FormData();
            form.append('file', blobInfo.blob(), blobInfo.filename());
            const resp = await fetch('/upload/image', {
                method: 'POST',
                body: form,
                headers: antiForgeryToken ? { 'RequestVerificationToken': antiForgeryToken } : {}
            });
            const json = await resp.json();
            if (!resp.ok || !json?.location) {
                throw new Error(json?.message || 'Image upload failed');
            }
            return json.location;
        },

        file_picker_callback: async (callback, value, meta) => {
            if (meta.filetype !== 'image' && meta.filetype !== 'media') return;

            const input = document.createElement('input');
            input.type = 'file';
            input.accept = meta.filetype === 'image'
                ? 'image/*'
                : 'video/mp4,video/webm,video/ogg';

            input.onchange = async () => {
                const file = input.files[0];
                const form = new FormData();
                form.append('file', file);
                const url = meta.filetype === 'image' ? '/upload/image' : '/upload/media';
                const resp = await fetch(url, {
                    method: 'POST',
                    body: form,
                    headers: antiForgeryToken ? { 'RequestVerificationToken': antiForgeryToken } : {}
                });
                const json = await resp.json();
                if (!resp.ok || !json?.location) {
                    alert(json?.message || 'Upload failed');
                    return;
                }
                callback(json.location, { title: file.name });
            };
            input.click();
        },

        setup: (editor) => {
            const box = document.getElementById('editor-word-count');
            const render = () => {
                const text = (editor.getContent({ format: 'text' }) || '').trim();
                const zhMatches = text.match(/[\p{Script=Han}]/gu) || [];
                const enMatches = text.match(/[A-Za-z]/g) || [];
                const total = text.replace(/\s/g, '').length;
                const otherCount = Math.max(total - zhMatches.length - enMatches.length, 0);

                box.innerHTML = `
                    <ul style="list-style:none;padding:0;margin:0;">
                        <li>🀄 中文字元：<strong>${zhMatches.length}</strong></li>
                        <li>🔤 英文字母：<strong>${enMatches.length}</strong></li>
                        <li>✨ 其他字元：<strong>${otherCount}</strong></li>
                        <li>📊 總字元（不含空白）：<strong>${total}</strong></li>
                    </ul>`;
            };

            editor.on('init keyup change setcontent', render);
            editor.on('init keyup change setcontent', () => {
                document.getElementById('live-preview').innerHTML = editor.getContent();
            });
        }
    });
});
