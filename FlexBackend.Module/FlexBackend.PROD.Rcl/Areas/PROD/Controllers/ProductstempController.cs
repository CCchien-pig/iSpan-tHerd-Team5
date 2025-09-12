using FlexBackend.Core.Interfaces.PROD;
using FlexBackend.Infra.Models;
using Microsoft.AspNetCore.Mvc;

namespace FlexBackend.PROD.Rcl.Areas.PROD.Controllers
{
	[Area("PROD")]
	public class ProductstempController : Controller
	{

		private readonly tHerdDBContext _db;

		public ProductstempController(tHerdDBContext repo)
		{
			_db = repo;
		}

        public async Task<IActionResult> Index_ex_datatable()
		{
			var products = _db.ProdProducts.ToList();

			SpecRandom();

			return View(products);
		}

		private void SpecRandom()
		{
			Random r = new Random();
			string[] groupNames = new string[] { "口味", "產地", "功率", "聯名", "贊助商", "原物料來源" };

			for (int i = 0; i < _db.ProdSpecificationConfigs.Count(); i++)
			{
				if (r.Next(100) < 30)
				{
					var groupName = groupNames[r.Next(groupNames.Length)];
					_db.ProdSpecificationConfigs.FirstOrDefault(x => x.SpecificationConfigId == 1000 + i).GroupName = groupName;
					_db.SaveChanges();
				}
			}
			

			for (int i = 1000; i < 1159; i++)
			{
				var insertCount = r.Next(4);
				for(int j=0;j<insertCount;j++)
				{
					var productId = _db.ProdProducts.FirstOrDefault(x => x.ProductId == i).ProductId;
					var config = new ProdSpecificationConfig()
					{
						ProductId = productId,
						GroupName = groupNames[r.Next(groupNames.Length)],
						OrderSeq = _db.ProdSpecificationConfigs.Where(x => x.ProductId == productId).Max(x => x.OrderSeq) + 1,
					};
					_db.ProdSpecificationConfigs.Add(config);
				}
			}
			_db.SaveChanges();
		}
	}
}
