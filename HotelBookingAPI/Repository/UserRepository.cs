using HotelBookingAPI.Connection;
using HotelBookingAPI.DTOs.UserDTOs;
using HotelBookingAPI.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Threading.Tasks;

namespace HotelBookingAPI.Repository
{
    public class UserRepository
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(SqlConnectionFactory connectionFactory, ILogger<UserRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<CreateUserResponseDTO> AddUserAsync(CreateUserDTO user)
        {
            var createUserResponseDTO = new CreateUserResponseDTO();
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var command = new SqlCommand("spAddUser", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@Email", user.Email);
                // Convert password to hash here 
                command.Parameters.AddWithValue("@PasswordHash", user.Password);
                command.Parameters.AddWithValue("@CreatedBy", "System");

                var userIdParam = new SqlParameter("@UserID", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                var errorMessageParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 255)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.Add(userIdParam);
                command.Parameters.Add(errorMessageParam);

                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                var userId = (int)userIdParam.Value;
                if (userId != -1)
                {
                    createUserResponseDTO.UserId = userId;
                    createUserResponseDTO.IsCreated = true;
                    createUserResponseDTO.Message = "User Created Successfully";
                    return createUserResponseDTO;
                }

                var message = errorMessageParam.Value?.ToString();
                createUserResponseDTO.IsCreated = false;
                createUserResponseDTO.Message = message ?? "An unknown error occurred while creating the user.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding a user.");
                createUserResponseDTO.IsCreated = false;
                createUserResponseDTO.Message = $"An error occurred: {ex.Message}";
            }

            return createUserResponseDTO;
        }

        // Similar changes should be applied to all other methods in the repository

        public async Task<UserRoleResponseDTO> AssignRoleToUserAsync(UserRoleDTO userRole)
        {
            var userRoleResponseDTO = new UserRoleResponseDTO();
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var command = new SqlCommand("spAssignUserRole", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@UserID", userRole.UserID);
                command.Parameters.AddWithValue("@RoleID", userRole.RoleID);

                var errorMessageParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 255)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.Add(errorMessageParam);

                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                var message = errorMessageParam.Value?.ToString();
                if (!string.IsNullOrEmpty(message))
                {
                    userRoleResponseDTO.Message = message;
                    userRoleResponseDTO.IsAssigned = false;
                }
                else
                {
                    userRoleResponseDTO.Message = "User Role Assigned";
                    userRoleResponseDTO.IsAssigned = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while assigning a role to the user.");
                userRoleResponseDTO.Message = $"An error occurred: {ex.Message}";
                userRoleResponseDTO.IsAssigned = false;
            }

            return userRoleResponseDTO;
        }

        // Similar changes should be applied to all other methods in the repository

        public async Task<List<UserResponseDTO>> ListAllUsersAsync(bool? isActive)
        {
            var users = new List<UserResponseDTO>();
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var command = new SqlCommand("spListAllUsers", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@IsActive", (object)isActive ?? DBNull.Value);

                await connection.OpenAsync().ConfigureAwait(false);
                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    users.Add(new UserResponseDTO
                    {
                        UserID = reader.GetInt32("UserID"),
                        Email = reader.GetString("Email"),
                        IsActive = reader.GetBoolean("IsActive"),
                        RoleID = reader.GetInt32("RoleID"),
                        LastLogin = reader.GetValueByColumn<DateTime?>("LastLogin"),
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while listing all users.");
            }

            return users;
        }

        public async Task<UserResponseDTO> GetUserByIdAsync(int userId)
        {
            UserResponseDTO user = null;
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var command = new SqlCommand("spGetUserByID", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@UserID", userId);
                var errorMessageParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };
                command.Parameters.Add(errorMessageParam);

                await connection.OpenAsync().ConfigureAwait(false);
                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

                if (await reader.ReadAsync().ConfigureAwait(false))
                {
                    user = new UserResponseDTO
                    {
                        UserID = reader.GetInt32("UserID"),
                        Email = reader.GetString("Email"),
                        IsActive = reader.GetBoolean("IsActive"),
                        RoleID = reader.GetInt32("RoleID"),
                        LastLogin = reader.GetValueByColumn<DateTime?>("LastLogin"),
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while getting the user by ID: {userId}");
            }

            return user;
        }

        public async Task<UpdateUserResponseDTO> UpdateUserAsync(UpdateUserDTO user)
        {
            var updateUserResponseDTO = new UpdateUserResponseDTO()
            {
                UserId = user.UserID
            };
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var command = new SqlCommand("spUpdateUserInformation", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@UserID", user.UserID);
                command.Parameters.AddWithValue("@Email", user.Email);
                command.Parameters.AddWithValue("@Password", user.Password);
                command.Parameters.AddWithValue("@ModifiedBy", "System");

                var errorMessageParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 255)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.Add(errorMessageParam);

                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                var message = errorMessageParam.Value?.ToString();
                if (string.IsNullOrEmpty(message))
                {
                    updateUserResponseDTO.Message = "User Information Updated.";
                    updateUserResponseDTO.IsUpdated = true;
                }
                else
                {
                    updateUserResponseDTO.Message = message;
                    updateUserResponseDTO.IsUpdated = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while updating the user with ID: {user.UserID}");
                updateUserResponseDTO.Message = $"An error occurred: {ex.Message}";
                updateUserResponseDTO.IsUpdated = false;
            }

            return updateUserResponseDTO;
        }

        public async Task<DeleteUserResponseDTO> DeleteUserAsync(int userId)
        {
            var deleteUserResponseDTO = new DeleteUserResponseDTO();
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var command = new SqlCommand("spToggleUserActive", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@UserID", userId);
                command.Parameters.AddWithValue("@IsActive", false);

                var errorMessageParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 255)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.Add(errorMessageParam);

                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                var message = errorMessageParam.Value?.ToString();
                if (!string.IsNullOrEmpty(message))
                {
                    deleteUserResponseDTO.Message = message;
                    deleteUserResponseDTO.IsDeleted = false;
                }
                else
                {
                    deleteUserResponseDTO.Message = "User Deleted.";
                    deleteUserResponseDTO.IsDeleted = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting the user with ID: {userId}");
                deleteUserResponseDTO.Message = $"An error occurred: {ex.Message}";
                deleteUserResponseDTO.IsDeleted = false;
            }

            return deleteUserResponseDTO;
        }

        public async Task<LoginUserResponseDTO> LoginUserAsync(LoginUserDTO login)
        {
            var userLoginResponseDTO = new LoginUserResponseDTO();
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var command = new SqlCommand("spLoginUser", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@Email", login.Email);
                command.Parameters.AddWithValue("@PasswordHash", login.Password); // Ensure password is hashed

                var userIdParam = new SqlParameter("@UserID", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                var errorMessageParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 255)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.Add(userIdParam);
                command.Parameters.Add(errorMessageParam);

                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                var success = userIdParam.Value != DBNull.Value && (int)userIdParam.Value > 0;
                if (success)
                {
                    var userId = Convert.ToInt32(userIdParam.Value);
                    userLoginResponseDTO.UserId = userId;
                    userLoginResponseDTO.IsLogin = true;
                    userLoginResponseDTO.Message = "Login Successful";
                }
                else
                {
                    var message = errorMessageParam.Value?.ToString();
                    userLoginResponseDTO.IsLogin = false;
                    userLoginResponseDTO.Message = message ?? "Login failed.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while logging in user with email: {login.Email}");
                userLoginResponseDTO.IsLogin = false;
                userLoginResponseDTO.Message = $"An error occurred: {ex.Message}";
            }

            return userLoginResponseDTO;
        }

        public async Task<(bool Success, string Message)> ToggleUserActiveAsync(int userId, bool isActive)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var command = new SqlCommand("spToggleUserActive", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@UserID", userId);
                command.Parameters.AddWithValue("@IsActive", isActive);

                var errorMessageParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 255) { Direction = ParameterDirection.Output };
                command.Parameters.Add(errorMessageParam);

                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);

                var message = errorMessageParam.Value?.ToString();
                var success = string.IsNullOrEmpty(message);
                return (success, message ?? "Operation completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while toggling active status for user with ID: {userId}");
                return (false, $"An error occurred: {ex.Message}");
            }
        }
    }
}
