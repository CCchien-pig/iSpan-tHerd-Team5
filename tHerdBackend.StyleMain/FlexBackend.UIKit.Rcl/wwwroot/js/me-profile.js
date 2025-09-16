(function(){
  function anti(){ return document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''; }
  function strong(p){ return /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/.test(p); }

  // 開啟 Modal：讀取自己的資料
  document.addEventListener('click', async (e)=>{
    if(!e.target.closest('#lnkMe')) return;
    e.preventDefault();

    const res = await fetch('/USER/Me/Details', { credentials: 'same-origin' });
    if(!res.ok){ alert('讀取個人資料失敗'); return; }
    const d = await res.json();

    document.getElementById('me_id').value      = d.id || '';
    document.getElementById('me_number').value  = d.number ?? '';
    document.getElementById('me_lastName').value= d.lastName ?? '';
    document.getElementById('me_firstName').value= d.firstName ?? '';
    document.getElementById('me_email').value   = d.email ?? '';
    document.getElementById('me_phone').value   = d.phoneNumber ?? '';
    document.getElementById('me_gender').value  = d.gender ?? 'N/A';
    document.getElementById('me_birth').value   = d.birthDate ?? '';
    document.getElementById('me_addr').value    = d.address ?? '';

    $('#dlgMe').modal('show'); // Bootstrap 4
  });

  // 儲存基本資料
  document.getElementById('frmMe').addEventListener('submit', async (e)=>{
    e.preventDefault();
    const fd = new FormData(e.target);
    const res = await fetch('/USER/Me/UpdateProfile', {
      method: 'POST',
      body: fd,
      headers: { 'RequestVerificationToken': anti() },
      credentials: 'same-origin'
    });
    const j = await res.json().catch(()=>({ ok:false }));
    if(res.ok && j.ok){
      $('#dlgMe').modal('hide');
      // 後端已 RefreshSignInAsync，右上角名稱會在下一次頁面渲染更新；
      // 若你想即時改 DOM，也可以自行更新 span 文字。
    }else{
      alert(j.message || '儲存失敗');
    }
  });

  // 變更密碼
  document.getElementById('btnMeChangePwd').addEventListener('click', async ()=>{
    const oldP = document.getElementById('me_oldPwd').value.trim();
    const p1   = document.getElementById('me_newPwd').value.trim();
    const p2   = document.getElementById('me_newPwd2').value.trim();

    if(!oldP || !p1 || !p2){ alert('請完整輸入舊密碼與新密碼'); return; }
    if(p1 !== p2){ alert('兩次新密碼不一致'); return; }
    if(!strong(p1)){ alert('新密碼需至少 8 碼且含英文大小寫與數字'); return; }

    const fd = new FormData(); fd.append('oldPassword', oldP); fd.append('newPassword', p1);
    const res = await fetch('/USER/Me/ChangePassword', {
      method:'POST',
      body: fd,
      headers: { 'RequestVerificationToken': anti() },
      credentials: 'same-origin'
    });
    const j = await res.json().catch(()=>({ ok:false }));
    if(res.ok && j.ok){
      document.getElementById('me_oldPwd').value='';
      document.getElementById('me_newPwd').value='';
      document.getElementById('me_newPwd2').value='';
      alert('密碼已更新');
    }else{
      alert(j.message || '變更密碼失敗');
    }
  });
})();
