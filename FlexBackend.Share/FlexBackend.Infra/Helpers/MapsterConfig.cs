using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Infra.Models;
using Mapster;

namespace ISpan.eMiniHR.DataAccess.Helpers
{
    public static class MapsterConfig
    {
        public static readonly TypeAdapterConfig Default = new();
        public static readonly TypeAdapterConfig Patch = new();
        private static bool _inited;

        public static void Register()
        {
            if (_inited) return; _inited = true;

            // ===== DTO -> Entity（一般覆蓋）=====
            Default.NewConfig<ProdProductDto, ProdProduct>()
                .Ignore(d => d.ProductId);

            // ===== Entity -> DTO =====
            Default.NewConfig<ProdProduct, ProdProductDto>();

            // ===== DTO -> Entity（PATCH：忽略 null）=====
            Patch.NewConfig<ProdProductDto, ProdProduct>()
                .IgnoreNullValues(true)      // 來源為 null 的欄位不覆蓋
                .Ignore(d => d.ProductId);
        }
    }
}