namespace src.Domain;

public static class Errors
{
    
    // VALIDATION
    
    public static Error Validation(string message) => new("Validation", message);
    public static Error Validation(string message, string field) => 
        new("Validation", message);

    
    // NOT FOUND
    
    public static Error NotFound(string entity, string id) => 
        new("NotFound", $"{entity} with ID '{id}' not found.");
    
    public static Error NotFound(string message) => new("NotFound", message);

    
    // AUTHORIZATION
    
    public static Error Forbidden(string message) => new("Authorization", message);
    public static Error Unauthorized(string message = "Invalid credentials.") => 
        new("Unauthorized", message);

    
    // CONFLICT
    
    public static Error Conflict(string message) => new("Conflict", message);

    
    // SERVER
    
    public static Error Internal(string message = "An unexpected error occurred. Please try again later.") => 
        new("ServerError", message);
}