using FlexBackend.Services.CNT;
using FlexBackend.Services.CS;
using FlexBackend.Services.MKT;
using FlexBackend.Services.ORD;
using FlexBackend.Services.PROD;
using FlexBackend.Services.SUP;
using FlexBackend.Services.USER;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlexBackend.Composition
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 方案級聚合註冊（只負責呼叫各模組的 AddXxx；自身不含業務）
        /// </summary>
        public static IServiceCollection AddFlexBackend(
        this IServiceCollection services,
        IConfiguration config,
        Action<FlexBackendOptions>? configure = null)
        {
            /*
                 IServiceCollection services：DI 容器，所有服務都會註冊到這裡。

                IConfiguration config：用來讀取 appsettings.json 裡的設定（連線字串、Storage、FeatureFlags）。

                Action<FlexBackendOptions>? configure：讓呼叫端（Host 專案）決定要不要啟用一些「專案級選項」，例如開發期功能（假資料、Swagger 等）。
             
             */

            var options = new FlexBackendOptions();
            configure?.Invoke(options);

            // (A) ConnectionStrings：用 GetConnectionString 最穩

            /*
                 用 AddOptions<T> 註冊 DbOptions 物件。

                使用 config.GetConnectionString("Default") 讀取設定檔 appsettings.json 裡 ConnectionStrings:Default 的值。

                這樣之後在 各模組的 Repository/Infra 只要注入 IOptions<DbOptions> 就能拿到連線字串，不需要每個模組都重複讀設定。
             */
            services.AddOptions<DbOptions>()
                    .Configure(o =>
                    {
                        o.Default = config.GetConnectionString("Default") ?? string.Empty;
                        o.ReadOnly = config.GetConnectionString("ReadOnly") ?? string.Empty;
                    });

            // (B) 一般區段：直接 Bind Section
            /*
                直接把 appsettings.json 裡的 Storage 與 Features 區段綁定到 StorageOptions、FeatureFlags。

                一樣可以在任何 Service 裡注入 IOptions<StorageOptions> 來讀取。
             */
            services.Configure<StorageOptions>(config.GetSection("Storage"));
            services.Configure<FeatureFlags>(config.GetSection("Features"));

            // 聚合註冊各模組
            services.AddSYSModule();     // 例如系統權限/程式碼表
            services.AddUSERDModule();   // 會員
            services.AddPRODModule();    // 商品/庫存
            services.AddORDModule();     // 訂單/金流
            services.AddMKTModule();     // 活動/優惠
            services.AddSUPModule();     // 供應商
            services.AddCNTModule();     // 內容
            services.AddCSModule();      // 客服

            return services;
        }
    }

    public sealed class FlexBackendOptions
    {
        /// <summary>是否註冊開發期專用服務（假資料/Seed/Swagger 等）</summary>
        public bool EnableDevFeatures { get; set; } = false;
    }

    // 專案常用的設定物件
    /*
        給資料庫連線用。

        之後 Infra 層的 SqlConnectionFactory 會用到。
     */
    public sealed class DbOptions
    {
        public string? Default { get; set; }
        public string? ReadOnly { get; set; }
    }

    // 存放檔案相關設定，例如上傳圖片要存到哪個路徑。
    public sealed class StorageOptions
    {
        public string? Assets { get; set; }
        public string? Images { get; set; }
    }

    /*
        控制功能開關（Feature Toggle）。

        例如要不要啟用「優惠券系統」或「廣告模組」 
    */
    public sealed class FeatureFlags
    {
        public bool EnableCoupons { get; set; }
        public bool EnableAdvertising { get; set; }
    }
}
