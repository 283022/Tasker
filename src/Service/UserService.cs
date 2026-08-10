using System.Runtime.InteropServices.Marshalling;
using Microsoft.AspNetCore.Mvc;
using src.Domain;
using src.Domain.Entities;
using src.Domain.ValueObjects;
using src.UnitOfWork;

namespace src;

public class UserService(IUnitOfWork unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public Result Register(CreateUserDto request)
    {
        if (request is null)
            return Result.Failure(
                new Error("Validation",
                    "Request cannot be null"));
        var email = Email.Create(request.Email);
        var  password = Password.Create(request.Password);

        if (email.IsFailure)
            return new Error(email.Error.Errortype, email.Error.Message);
        if(password.IsFailure)
            return new Error(password.Error.Errortype, password.Error.Message);
        
        var user = User.Create(request.Name,email.Value, password.Value);
        
        if (user.IsFailure) return Result.Failure(user.Error);
        if (user.Value is null) return Result.Failure(new Error("User isn't exist", "User doesn't exist"));
        
        if (_unitOfWork.UserRepository.UserExists(user.Value.Email.Value))
            return Result.Failure(
                new Error("Conflict",
                    $"User with email {request.Email} already exists"));
        
        
        _unitOfWork.UserRepository.Add(user.Value);
        _unitOfWork.SaveChanges();
        
        return Result.Success();
        
    }

    private bool UserExists(string email)
    {
        return _unitOfWork.UserRepository.UserExists(email);
    }

    public Result<User> Userlogin(LoginUserDto request)
    {
        if (request is null)
            return Result<User>.Failure(new Error("Validation","Request cannot be null"));
        var email = Email.Create(request.Email);
        if (email.IsFailure)
        {
            return new Error("Validation", email.Value.Value + " isn't valid");
        }
        var user = _unitOfWork.UserRepository.GetUserByEmail(email.Value.Value);

        if (user is null)
        {
            var error = new Error("UserNotFounded", email.Value.Value + " not found");
            return error;
        }

        var password = Password.Create(request.Password);
        if (user.Password.Value != password.Value.Value)
            return Result<User>.Failure(new Error("Unauthorized","Invalid credentials"));

        return Result<User>.Success(user);
    }

    public Result<Guid> GetUserId(LoginUserDto request)
    {
        if (request is null)
            return Result<Guid>.Failure(new Error("Validation","Request cannot be null"));

        var user = _unitOfWork.UserRepository.GetUserByEmail(request.Email);
        if (user is null)
            return Result<Guid>.Failure(new Error("NotFound", request.Email + "not found"));

        return Result<Guid>.Success(user.Id);
    }

    public Result<User> GetUserById(Guid id)
    {
        if (id == Guid.Empty)
            return new Error("Validation", "Id cannot be empty");
        
        var user = _unitOfWork.UserRepository.GetUserById(id);
        if (user is null)
            return new Error("Validation", "User not found");
        return Result<User>.Success(user);
    }

    public Result DeleteUser(DeleteUserDto request, Guid userid)
    {
        if (userid == Guid.Empty)
            return new Error("Validation", "Id cannot be empty");
        if (request is null)
            return new Error("Validation", "Request cannot be null");
        if (request.id == Guid.Empty)
            return new Error("Validation", "Id cannot be null");
        
        
        var user = _unitOfWork.UserRepository.GetUserById(request.id);
        if (user is null)
            return new Error("Validation", "User is not founded");
        
        if (user.Id != userid)
            return new Error("Authorization", "U cannot delete user");
        
        _unitOfWork.UserRepository.Delete(user);
        _unitOfWork.SaveChanges();
        
        return Result.Success();
    }

}