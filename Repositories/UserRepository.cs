using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Queue.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace Queue.Repositories
{
    public class UserRepository
    {
        private readonly ApplicationDbContext _dbContext;
        static string secretKey = "super_super_secret_key_with_256_bits_!!!"; // 256+ бит
        static string issuer = "MyApp";
        static string audience = "MyUsers";
        static SymmetricSecurityKey signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        public UserRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User> Add(string username, string password, string email, int groupId, int subgroupNumber)
        {
            // Создайте экземпляр PasswordHasher
            var passwordHasher = new PasswordHasher<User>();
            if (_dbContext.Users.Select(x => x.UserName).ToList().Contains(username)) return null;
            // Хэшируйте пароль
            var hashedPassword = passwordHasher.HashPassword(null, password);

            var user = new User
            {
                UserName = username,
                // Сохраните только хэшированный пароль
                Password = hashedPassword,
                Email = email,
                GroupId = groupId,
                SubgroupNumber = subgroupNumber
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }


        public async Task<User> GetUser(string Password, string Email)
        {
            var passwordHasher = new PasswordHasher<User>();

            // Хэшируйте пароль
            //var hashedPassword = passwordHasher.HashPassword(null, Password);

            User user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == Email);
            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.Password, Password);
            //bool a = user.Password == hashedPassword;
            return verificationResult == PasswordVerificationResult.Success ? user : null;
        }



    }
}
