using ClaimSystemMicroAPI.Data;
using ClaimSystemMicroAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimSystemMicroAPI.Services
{
    public class AuthService
    {
        private readonly ClaimAPIDbContext _context;

        public AuthService(ClaimAPIDbContext context)
        {
            _context = context;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest lr)
        {
            string? ipAddress = null;
            string? userAgent = null;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == lr.UserName && u.IsActive);

            if (user == null)
                return Failed("Username is invalid");

            if (lr.Password == null || !BCrypt.Net.BCrypt.Verify(lr.Password, user.PasswordHash))
                return Failed("Password is invalid");


            var userRoles = await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == user.UserId)
                .Select(ur => ur.Role.RoleName)
                .ToListAsync();

            var session = new Session
            {
                SessionId = Guid.NewGuid().ToString("N"),
                UserId = user.UserId,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddHours(20),
                IsActive = true,
                IPAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();


            return LoginSuccess("Login successful", session.SessionId, user, userRoles);
        }

        public AuthResponse Failed(string message)
        {
            return new AuthResponse { Success = false, Message = message };
        }

        public AuthResponse LoginSuccess(string message, string sessionId, User user, List<string> userRoles)
        {
            return new AuthResponse
            {
                Success = true,
                Message = message,
                SessionId = sessionId,
                User = new UserDto
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    ContactNumber = user.ContactNumber,
                    Address = user.Address,
                    Faculty = user.Faculty,
                    HourlyRate = user.HourlyRate,
                    MaxHours = user.MaxHours,
                    IsActive = user.IsActive,
                    CreatedDate = user.CreatedDate,
                    Roles = userRoles
                }
            };
        }



        public async Task<AuthResponse> ValidateSessionAsync(string sessionId)
        {

            var session = await _context.Sessions.Include(s => s.User).FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);

            if (session.ExpiryDate < DateTime.Now)
            {
                return Failed("Session has expired");
            }

            var userRoles = await _context.UserRoles.Include(ur => ur.Role).Where(ur => ur.UserId == session.UserId).Select(ur => ur.Role.RoleName).ToListAsync();

            return SessionValid("Session is valid", sessionId, session, userRoles);
        }


        public AuthResponse SessionValid(string message, string sessionId, Session session, List<string> userRoles)
        {
            return new AuthResponse
            {
                Success = true,
                Message = message,
                SessionId = sessionId,
                User = new UserDto
                {
                    UserId = session.User.UserId,
                    UserName = session.User.UserName,
                    Email = session.User.Email,
                    FirstName = session.User.FirstName,
                    LastName = session.User.LastName,
                    ContactNumber = session.User.ContactNumber,
                    Address = session.User.Address,
                    Faculty = session.User.Faculty,
                    HourlyRate = session.User.HourlyRate,
                    MaxHours = session.User.MaxHours,
                    IsActive = session.User.IsActive,
                    CreatedDate = session.User.CreatedDate,
                    Roles = userRoles
                }
            };
        }

        public async Task<bool> LogoutAsync(string sessionId)
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);
            if (session == null)
            {
                return false;
            }
            session.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AuthResponse> CreateUserAsync(CreateUserRequest request)
        {


            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == request.UserName || u.Email == request.Email);

            if (existingUser != null)
            {
                return Failed("Username or email already exists");
            }

            var validRoles = await _context.Roles
                .Where(r => request.RoleIds.Contains(r.RoleId))
                .ToListAsync();

            if (validRoles.Count != request.RoleIds.Count)
            {
                return Failed("One or more invalid role IDs");
            }



            var user = new User
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                ContactNumber = request.ContactNumber,
                Address = request.Address,
                Faculty = request.Faculty,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                HourlyRate = request.HourlyRate,
                MaxHours = request.MaxHours,
                IsActive = true,
                CreatedDate = DateTime.Now
            };


            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            // Assign roles
            foreach (var roleId in request.RoleIds)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = roleId
                });
            }

            await _context.SaveChangesAsync();

            var roleNames = validRoles.Select(r => r.RoleName).ToList();

            return UserSuccess("User created successfully", user, roleNames);
        }




        public AuthResponse UserSuccess(string message, User user, List<string> roleNames)
        {
            return new AuthResponse
            {
                Success = true,
                Message = message,
                User = new UserDto
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    ContactNumber = user.ContactNumber,
                    Address = user.Address,
                    Faculty = user.Faculty,
                    HourlyRate = user.HourlyRate,
                    MaxHours = user.MaxHours,
                    IsActive = user.IsActive,
                    CreatedDate = user.CreatedDate,
                    Roles = roleNames
                },
            };
        }


        // Update user
        public async Task<AuthResponse> UpdateUserAsync(UpdateUserRequest request)
        {
            var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == request.UserId);

            if (user == null)
            {
                return Failed("User not found");
            }


            var conflict = await _context.Users.FirstOrDefaultAsync(u => u.UserId != request.UserId && (u.UserName == request.UserName || u.Email == request.Email));

            if (conflict != null)
            {
                return Failed("Username or email already in use by another user");
            }


            if (request.RoleIds.Any())
            {

                var existingRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == user.UserId)
                    .ToListAsync();
                _context.UserRoles.RemoveRange(existingRoles);


                foreach (var roleId in request.RoleIds)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = roleId
                    });
                }
            }
            _context.Update(user);
            await _context.SaveChangesAsync();

            // Get updated roles
            var userRoles = await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == user.UserId)
                .Select(ur => ur.Role.RoleName)
                .ToListAsync();

            return UserSuccess("User updated successfully", user, userRoles);
        }




        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {

            var user = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    ContactNumber = u.ContactNumber,
                    Address = u.Address,
                    Faculty = u.Faculty,
                    HourlyRate = u.HourlyRate,
                    MaxHours = u.MaxHours,
                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedDate,
                    Roles = _context.UserRoles
                        .Where(ur => ur.UserId == u.UserId)
                        .Select(ur => ur.Role.RoleName)
                        .ToList()
                })
                .FirstOrDefaultAsync();

            return user;
        }


        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    ContactNumber = u.ContactNumber,
                    Address = u.Address,
                    Faculty = u.Faculty,
                    HourlyRate = u.HourlyRate,
                    MaxHours = u.MaxHours,
                    Roles = _context.UserRoles
                        .Where(ur => ur.UserId == u.UserId)
                        .Select(ur => ur.Role.RoleName)
                        .ToList(),

                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedDate
                })
                .ToListAsync();

            return users;
        }


        // Get all roles
        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Roles.ToListAsync();
        }
        // Change password
        public async Task<AuthResponse> ChangePasswordAsync(ChangePasswordRequest request)
        {

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == request.UserId);

            if (user == null)
            {
                return Failed("User not found");
            }


            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return Failed("Current password is incorrect");
            }


            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return UserSuccess("Password changed successfully", user, new List<string>());
        }

    }
}


