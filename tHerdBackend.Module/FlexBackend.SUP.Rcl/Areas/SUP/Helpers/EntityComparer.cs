namespace SUP.Data.Helpers
{
	public static class EntityComparer
	{
		/// <summary>
		/// 比對兩個物件所有同名屬性是否完全相同
		/// </summary>
		// Controller 使用範例：
		// if (EntityComparer.IsUnchanged(
		//		entity, model,
		//		nameof(entity1, model1),
		//		nameof(entity2, model2),...)))
		//	{	return Json(new { success = false, message = "未變更" });}
		public static bool IsUnchanged<T1, T2>(T1 obj1, T2 obj2, params string[] compareProperties)
		{
			var props2 = typeof(T2).GetProperties().ToDictionary(p => p.Name);

			foreach (var propName in compareProperties)
			{
				var prop1 = typeof(T1).GetProperty(propName);
				if (prop1 == null) continue;
				if (!props2.TryGetValue(propName, out var prop2)) continue;

				var val1 = prop1.GetValue(obj1);
				var val2 = prop2.GetValue(obj2);

				if (!object.Equals(val1, val2))
					return false;
			}

			return true;
		}

	}
}
