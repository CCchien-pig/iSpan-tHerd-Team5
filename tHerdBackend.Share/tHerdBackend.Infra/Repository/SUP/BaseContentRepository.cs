using tHerdBackend.Infra.Models;

namespace tHerdBackend.Infra.Repository.SUP
{
	public abstract class BaseContentRepository<TEntity, TDto>
		where TEntity : class
		where TDto : class
	{
		protected readonly tHerdDBContext _context;

		public BaseContentRepository(tHerdDBContext context)
		{
			_context = context;
		}

		// 抽象方法：必須由子類別實作，告訴 Repository 如何將 Entity 投影成 DTO
		protected abstract IQueryable<TDto> ProjectToDto(IQueryable<TEntity> entities);

		// 抽象方法：必須由子類別實作，告訴 Repository 如何將 DTO 映射到現有的 Entity
		protected abstract void MapDtoToEntity(TDto dto, TEntity entity);
	}
}
