using Moq;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace DataAccess.Tests.Util
{
    public static class TestUtil
    {
        public static Mock<DbSet<T>> GetQueryableMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var dbSet = new Mock<DbSet<T>>();

            dbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            dbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            dbSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>((s) => sourceList.Add(s));

            dbSet.Setup(d => d.Include(It.IsAny<string>())).Returns(dbSet.Object);
            dbSet.Setup(d => d.AsNoTracking()).Returns(dbSet.Object);

            return dbSet;
        }
    }
}
