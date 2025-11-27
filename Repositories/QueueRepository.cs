using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Queue.Models;
using System.Text;

namespace Queue.Repositories
{
    public class QueueRepository
    {
        private readonly ApplicationDbContext _dbContext;
        
        public QueueRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> Add(int userId, int dateId, int numberUser)
        {
            
            if (userId <= 0 || dateId <= 0 || numberUser <= 0)
            {
                return false; 
            }

            if (_dbContext.Queue.FirstOrDefault(x => x.UserId == userId && x.DateId == dateId && x.NumberUser == numberUser) != null) return false;
            
            var queue = new Queue.Models.Queue
            {
                DateId = dateId,
                NumberUser = numberUser,
                UserId = userId
            };

            try
            {
                
                await _dbContext.Queue.AddAsync(queue);

                
                await _dbContext.SaveChangesAsync();

                return true; 
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error occurred while adding to queue: {ex.Message}");

                return false; // Неудача
            }
        }

        public async Task<bool> Delete(int queueId)
        {
            var queue = await _dbContext.Queue.FindAsync(queueId);
            if (queue == null) return false;

            _dbContext.Queue.Remove(queue);
            await _dbContext.SaveChangesAsync();

            return true;
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
