using src;
using src.Mappers;

namespace TestProject1.Integration_tests;

public class TaskServiceTests
{
    private readonly TaskRepository _realTaskRepository;
    private readonly MapperTaskFromDto _realMapper;
    private readonly TaskService _sut;
    
    public TaskServiceTests()
    {
        _realTaskRepository = new TaskRepository();
        _realMapper = new MapperTaskFromDto();
        _sut = new TaskService(_realTaskRepository, _realMapper);
    }
    
    [Fact]
    public void Create_returns_task_id_when_task_is_valid()
    {
        // Arrange
        var createTaskDto = new CreateTaskDto(Guid.NewGuid(), "Complete project report");
        
        // Act
        var result = _sut.Create(createTaskDto);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        
        var savedTask = _realTaskRepository.GetTaskById(result.Value);
        Assert.NotNull(savedTask);
        Assert.Equal("Complete project report", savedTask.Name);
        Assert.Equal(createTaskDto.userid, savedTask.CreateByUserID);
    }
    
    [Fact]
    public void Create_generates_unique_id_for_each_task()
    {
        // Arrange
        var dto1 = new CreateTaskDto(Guid.NewGuid(), "Task 1");
        var dto2 = new CreateTaskDto(Guid.NewGuid(), "Task 2");
        
        // Act
        var result1 = _sut.Create(dto1);
        var result2 = _sut.Create(dto2);
        
        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.Value, result2.Value);
    }
    
    [Fact]
    public void Create_accepts_task_with_same_name_for_different_users()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var dto1 = new CreateTaskDto(userId1, "Same Task Name");
        var dto2 = new CreateTaskDto(userId2, "Same Task Name");
        
        // Act
        var result1 = _sut.Create(dto1);
        var result2 = _sut.Create(dto2);
        
        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.Value, result2.Value);
    }
    
    [Fact]
    public void Delete_removes_task_when_task_exists()
    {
        // Arrange
        var createDto = new CreateTaskDto(Guid.NewGuid(), "Task to delete");
        var createResult = _sut.Create(createDto);
        Assert.True(createResult.IsSuccess);
        
        var deleteDto = new DeleteTaskDto(createResult.Value);
        
        // Act
        var result = _sut.Delete(deleteDto);
        
        // Assert
        Assert.True(result.IsSuccess);
        
        var deletedTask = _realTaskRepository.GetTaskById(createResult.Value);
        Assert.Null(deletedTask);
    }
    
    [Fact]
    public void GetTask_returns_task_when_task_exists()
    {
        // Arrange
        var createDto = new CreateTaskDto(Guid.NewGuid(), "Task to retrieve");
        var createResult = _sut.Create(createDto);
        Assert.True(createResult.IsSuccess);
        
        // Act
        var result = _sut.GetTask(createResult.Value);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Task to retrieve", result.Value.Name);
        Assert.Equal(createResult.Value, result.Value.Id);
        Assert.Equal(createDto.userid, result.Value.CreateByUserID);
    }
    
    [Fact]
    public void Multiple_tasks_can_be_created_and_retrieved()
    {
        // Arrange
        var dto1 = new CreateTaskDto(Guid.NewGuid(), "Task 1");
        var dto2 = new CreateTaskDto(Guid.NewGuid(), "Task 2");
        var dto3 = new CreateTaskDto(Guid.NewGuid(), "Task 3");
        
        // Act
        var result1 = _sut.Create(dto1);
        var result2 = _sut.Create(dto2);
        var result3 = _sut.Create(dto3);
        
        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.True(result3.IsSuccess);
        
        var task1 = _realTaskRepository.GetTaskById(result1.Value);
        var task2 = _realTaskRepository.GetTaskById(result2.Value);
        var task3 = _realTaskRepository.GetTaskById(result3.Value);
        
        Assert.NotNull(task1);
        Assert.NotNull(task2);
        Assert.NotNull(task3);
        Assert.NotEqual(task1.Id, task2.Id);
        Assert.NotEqual(task2.Id, task3.Id);
        Assert.NotEqual(task1.Id, task3.Id);
    }
    
    // ============================================================
    // 2. НЕГАТИВНЫЕ ТЕСТЫ (Validation Errors)
    // ============================================================
    
    [Fact]
    public void Create_returns_failure_when_dto_is_null()
    {
        // Act
        var result = _sut.Create(null);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Request cannot be null", result.Error.Message);
        
        var allTasks = _realTaskRepository.GetAllTasks();
        Assert.Empty(allTasks);
    }
    
    [Fact]
    public void Create_returns_failure_when_name_is_empty()
    {
        // Arrange
        var createDto = new CreateTaskDto(Guid.NewGuid(), "");
        
        // Act
        var result = _sut.Create(createDto);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Name cannot be null or empty", result.Error.Message);
        
        var allTasks = _realTaskRepository.GetAllTasks();
        Assert.Empty(allTasks);
    }
    
    [Fact]
    public void Create_returns_failure_when_name_is_whitespace()
    {
        // Arrange
        var createDto = new CreateTaskDto(Guid.NewGuid(), "   ");
        
        // Act
        var result = _sut.Create(createDto);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Name cannot be null or empty", result.Error.Message);
        
        var allTasks = _realTaskRepository.GetAllTasks();
        Assert.Empty(allTasks);
    }
    
    [Fact]
    public void Create_returns_failure_when_name_is_null()
    {
        // Arrange
        var createDto = new CreateTaskDto(Guid.NewGuid(), null);
        
        // Act
        var result = _sut.Create(createDto);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Name cannot be null or empty", result.Error.Message);
        
        var allTasks = _realTaskRepository.GetAllTasks();
        Assert.Empty(allTasks);
    }
    
    [Fact]
    public void Create_returns_failure_when_user_id_is_empty()
    {
        // Arrange
        var createDto = new CreateTaskDto(Guid.Empty, "Task");
        
        // Act
        var result = _sut.Create(createDto);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("CreateByUserId cannot be empty", result.Error.Message);
        
        var allTasks = _realTaskRepository.GetAllTasks();
        Assert.Empty(allTasks);
    }
    
    [Fact]
    public void Delete_returns_failure_when_dto_is_null()
    {
        // Act
        var result = _sut.Delete(null);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Request cannot be null", result.Error.Message);
    }
    
    [Fact]
    public void Delete_returns_failure_when_task_does_not_exist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var deleteDto = new DeleteTaskDto(nonExistentId);
        
        // Act
        var result = _sut.Delete(deleteDto);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal($"Task ID isn't correct", result.Error.Message);
    }
    
    [Fact]
    public void Delete_returns_failure_when_task_id_is_empty()
    {
        // Arrange
        var deleteDto = new DeleteTaskDto(Guid.Empty);
        
        // Act
        var result = _sut.Delete(deleteDto);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Task ID isn't correct", result.Error.Message);
    }
    
    [Fact]
    public void GetTask_returns_failure_when_id_is_empty()
    {
        // Act
        var result = _sut.GetTask(Guid.Empty);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Task ID cannot be empty", result.Error.Message);
    }
    
    [Fact]
    public void GetTask_returns_failure_when_task_does_not_exist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        
        // Act
        var result = _sut.GetTask(nonExistentId);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal($"Task:{nonExistentId.ToString()}", result.Error.Message);
    }
    
    // ============================================================
    // 3. ГРАНИЧНЫЕ ТЕСТЫ
    // ============================================================
    
    [Fact]
    public void Create_accepts_task_with_long_name()
    {
        // Arrange
        var longName = new string('a', 1000);
        var createDto = new CreateTaskDto(Guid.NewGuid(), longName);
        
        // Act
        var result = _sut.Create(createDto);
        
        // Assert
        Assert.True(result.IsSuccess);
        var savedTask = _realTaskRepository.GetTaskById(result.Value);
        Assert.NotNull(savedTask);
        Assert.Equal(longName, savedTask.Name);
    }
    
    [Fact]
    public void Create_accepts_task_with_special_characters_in_name()
    {
        // Arrange
        var specialName = "Task!@#$%^&*()_+-=[]{};':\",./<>?";
        var createDto = new CreateTaskDto(Guid.NewGuid(), specialName);
        
        // Act
        var result = _sut.Create(createDto);
        
        // Assert
        Assert.True(result.IsSuccess);
        var savedTask = _realTaskRepository.GetTaskById(result.Value);
        Assert.NotNull(savedTask);
        Assert.Equal(specialName, savedTask.Name);
    }
    
    [Fact]
    public void Tasks_with_same_name_and_same_user_are_both_created()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto1 = new CreateTaskDto(userId, "Same Task");
        var dto2 = new CreateTaskDto(userId, "Same Task");
        
        // Act
        var result1 = _sut.Create(dto1);
        var result2 = _sut.Create(dto2);
        
        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.Value, result2.Value);
        
        var task1 = _realTaskRepository.GetTaskById(result1.Value);
        var task2 = _realTaskRepository.GetTaskById(result2.Value);
        Assert.NotNull(task1);
        Assert.NotNull(task2);
        Assert.Equal("Same Task", task1.Name);
        Assert.Equal("Same Task", task2.Name);
    }
    
    [Fact]
    public void Delete_with_valid_id_does_not_affect_other_tasks()
    {
        // Arrange
        var dto1 = new CreateTaskDto(Guid.NewGuid(), "Task 1");
        var dto2 = new CreateTaskDto(Guid.NewGuid(), "Task 2");
        
        var result1 = _sut.Create(dto1);
        var result2 = _sut.Create(dto2);
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        
        var deleteDto = new DeleteTaskDto(result1.Value);
        
        // Act
        var deleteResult = _sut.Delete(deleteDto);
        
        // Assert
        Assert.True(deleteResult.IsSuccess);
        
        var deletedTask = _realTaskRepository.GetTaskById(result1.Value);
        var remainingTask = _realTaskRepository.GetTaskById(result2.Value);
        
        Assert.Null(deletedTask);
        Assert.NotNull(remainingTask);
        Assert.Equal("Task 2", remainingTask.Name);
    }
}