using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodenamesServer
{
    public class Class1
    {
        public Class1()
        {
            TestDB();
        }
        private void TestDB()
        {
            User newUser = new User();
            newUser.email = "test@mail.com";
            newUser.password = "password";

            //Add
            using (var dbContext = new codenamesEntities())
            {
                dbContext.User.Add(newUser);
                dbContext.SaveChanges();
            }

            using (var dbContext = new codenamesEntities())
            {
                var query = from user in dbContext.User select user;
                
                foreach (var item in query)
                {
                    Console.WriteLine(item.userID+" "+item.email);
                }
            }

            using (var dbContext = new codenamesEntities())
            {
                var user = (from u in dbContext.User
                            where u.email == "test@mail.com"
                            select u).Single();
                user.email = "updatedTest@mail.com";
                dbContext.SaveChanges();
            }

            using (var dbContext = new codenamesEntities())
            {
                var query = from user in dbContext.User select user;

                foreach (var item in query)
                {
                    Console.WriteLine(item.userID + " " + item.email);
                }
            }

            using (var dbContext = new codenamesEntities())
            {
                var deletedUser = (from u in dbContext.User
                                   where u.email == "updatedTest@mail.com"
                                   select u).Single();
                dbContext.User.Remove(deletedUser);
                dbContext.SaveChanges();
            }
        }
    }
}
