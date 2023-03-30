using Microsoft.AspNetCore.Identity;

namespace CatScale.Service.DbModel;

public static class UserDbInitializer
{
    public static async Task Initialize(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        const string adminUserName = "Admin";
        const string adminRoleName = ApplicationRoles.Admin;
        
        var existingAdminUser = await userManager.FindByNameAsync(adminUserName);
        if (existingAdminUser is null)
        {
            Console.WriteLine($"Creating admin user ...");

            var result = await userManager.CreateAsync(new ApplicationUser()
            {
                UserName = adminUserName,
                LockoutEnabled = false,
            }, "password");

            if (result.Succeeded)
            {
                existingAdminUser = await userManager.FindByNameAsync(adminUserName);
            }
            else
            {
                Console.WriteLine($"Failed to create admin user:");
                foreach (var error in result.Errors)
                    Console.WriteLine($"- {error.Code} {error.Description}");
            }
        }
        else
        {
            Console.WriteLine($"Admin user already exists");
        }

        var existingAdminRole = await roleManager.FindByNameAsync(adminRoleName);
        if (existingAdminRole is null)
        {
            Console.WriteLine($"Creating admin role ...");

            var result = await roleManager.CreateAsync(new ApplicationRole()
            {
                Name = adminRoleName,
            });

            if (result.Succeeded)
            {
                existingAdminRole = await roleManager.FindByNameAsync(adminRoleName);
            }
            else
            {
                Console.WriteLine($"Failed to create admin role:");
                foreach (var error in result.Errors)
                    Console.WriteLine($"- {error.Code} {error.Description}");
            }
        }
        else
        {
            Console.WriteLine($"Admin role already exists");
        }
        
        if (existingAdminUser is not null && existingAdminRole is not null)
        {
            if (await userManager.IsInRoleAsync(existingAdminUser, adminRoleName) == false)
            {
                Console.WriteLine($"Adding admin user to admin role ...");
                
                var result = await userManager.AddToRoleAsync(existingAdminUser, adminRoleName);

                if (!result.Succeeded)
                {
                    Console.WriteLine($"Failed to add admin user to admin role:");
                    foreach (var error in result.Errors)
                        Console.WriteLine($"- {error.Code} {error.Description}");
                }
            }
            else
            {
                Console.WriteLine($"Admin user is already in admin role");
            }
        }
        else
        {
            Console.WriteLine($"Can't add admin user to admin role because something went wrong earlier");
        }
        
        // --------

        Console.WriteLine($"Users:");
        foreach (var user in userManager.Users.ToArray())
        {
            var roles = await userManager.GetRolesAsync(user);
            Console.WriteLine($"- {user.Id} {user.UserName} (Roles: {String.Join(",", roles)})");
        }
        Console.WriteLine($"Roles:");
        foreach (var role in roleManager.Roles.ToArray())
        {
            Console.WriteLine($"- {role.Id} {role.Name}");
        }
    }
}