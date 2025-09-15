using FlexBackend.Core.DTOs.PROD;
using FlexBackend.Infra.Models;
using Mapster;

public static class MapsterConfig
{
    public static readonly TypeAdapterConfig Default = new();
    public static readonly TypeAdapterConfig Patch = new();
    private static bool _inited;

    public static void Register()
    {
        if (_inited) return; _inited = true;

        // ========== DTO -> Entity：Create 用 ==========
        Default.NewConfig<ProdProductDto, ProdProduct>()
            .Ignore(d => d.ProductId)
            .Ignore(d => d.Creator)
            .Ignore(d => d.CreatedDate)
            .Ignore(d => d.Reviser)
            .Ignore(d => d.RevisedDate);

        // ========== Entity -> DTO ==========
        Default.NewConfig<ProdProduct, ProdProductDto>();

        // ========== DTO -> Entity：PATCH 用（忽略 null）==========
        Patch.NewConfig<ProdProductDto, ProdProduct>()
            .IgnoreNullValues(true)
            .Ignore(d => d.ProductId)
            .Ignore(d => d.Creator)
            .Ignore(d => d.CreatedDate)
            .Ignore(d => d.Reviser)
            .Ignore(d => d.RevisedDate);


        // ========== DTO -> Entity：Create 用 ==========
        Default.NewConfig<ProdProductSkuDto, ProdProductSku>()
            .Ignore(d => d.SkuCode);

        // ========== Entity -> DTO ==========
        Default.NewConfig<ProdProductSku, ProdProductSkuDto>();

        // ========== DTO -> Entity：PATCH 用（忽略 null）==========
        Patch.NewConfig<ProdProductSkuDto, ProdProductSku>()
            .IgnoreNullValues(true)
            .Ignore(d => d.SkuCode);
    }
}
