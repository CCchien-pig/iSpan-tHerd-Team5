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
        if (_inited) return;
        _inited = true;

        // ========== ProdProduct ==========

        // Create: DTO -> Entity
        Default.NewConfig<ProdProductDto, ProdProduct>()
            .Ignore(d => d.ProductId)
            .Ignore(d => d.Creator)
            .Ignore(d => d.CreatedDate)
            .Ignore(d => d.Reviser)
            .Ignore(d => d.RevisedDate);

        // Entity -> DTO
        Default.NewConfig<ProdProduct, ProdProductDto>();

        // Patch: DTO -> Entity
        Patch.NewConfig<ProdProductDto, ProdProduct>()
            .IgnoreNullValues(true)
            .Ignore(d => d.ProductId)
            .Ignore(d => d.Creator)
            .Ignore(d => d.CreatedDate)
            .Ignore(d => d.Reviser)
            .Ignore(d => d.RevisedDate);

        // ========== ProdProductSku ==========

        // Create: DTO -> Entity
        Default.NewConfig<ProdProductSkuDto, ProdProductSku>()
            .Ignore(d => d.SkuId)    // PK 通常由 DB identity 產生
            .Ignore(d => d.SkuCode); // 如果 SkuCode 要後端自動生成

        // Entity -> DTO
        Default.NewConfig<ProdProductSku, ProdProductSkuDto>();

        // Patch: DTO -> Entity
        Patch.NewConfig<ProdProductSkuDto, ProdProductSku>()
            .IgnoreNullValues(true)
            .Ignore(d => d.SkuId)
            .Ignore(d => d.SkuCode);
    }
}
