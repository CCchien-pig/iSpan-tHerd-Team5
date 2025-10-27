using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tHerdBackend.Core.Dtos
{
	public class FoodCategoryDto
	{
		public int Id { get; set; }          // CategoryId
		public string Name { get; set; } = ""; // CategoryName
	}
}
