using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

public class TelecomAntRoutingTests : IDisposable
{
    // Kopia zapasowa oryginalnego stanu statycznego do przywrócenia po testach
    private readonly double[,] _originalDelays;
    private readonly double[,] _originalPheromones;
    private readonly int _originalNumRouters;
    private readonly int _originalSourceRouter;
    private readonly int _originalDestRouter;

    public TelecomAntRoutingTests()
    {
        _originalDelays = (double[,])GetStaticField("delays");
        _originalPheromones = (double[,])GetStaticField("pheromones");
        _originalNumRouters = (int)GetStaticField("numRouters");
        _originalSourceRouter = (int)GetStaticField("sourceRouter");
        _originalDestRouter = (int)GetStaticField("destRouter");
    }

    public void Dispose()
    {
        // Przywracanie stanu po każdym teście
        SetStaticField("delays", _originalDelays);
        SetStaticField("pheromones", _originalPheromones);
        SetStaticField("numRouters", _originalNumRouters);
        SetStaticField("sourceRouter", _originalSourceRouter);
        SetStaticField("destRouter", _originalDestRouter);
    }

    #region Helper Methods for Reflection
    private static object GetStaticField(string fieldName)
    {
        var field = typeof(TelecomAntRouting).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        return field.GetValue(null);
    }

    private static void SetStaticField(string fieldName, object value)
    {
        var field = typeof(TelecomAntRouting).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        field.SetValue(null, value);
    }

    private static object InvokeStaticMethod(string methodName, params object[] parameters)
    {
        var method = typeof(TelecomAntRouting).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        return method.Invoke(null, parameters);
    }
    #endregion

    [Fact]
    public void InitializePheromones_ShouldSetInitialValueToOne_WhenDelayIsGreaterThanZero()
    {
        // Arrange
        double[,] customDelays = new double[,]
        {
            { 0, 10 },
            { 10, 0 }
        };
        SetStaticField("numRouters", 2);
        SetStaticField("delays", customDelays);
        SetStaticField("pheromones", new double[2, 2]);

        // Act
        InvokeStaticMethod("InitializePheromones");

        // Assert
        double[,] pheromones = (double[,])GetStaticField("pheromones");
        Assert.Equal(1.0, pheromones[0, 1]);
        Assert.Equal(1.0, pheromones[1, 0]);
        Assert.Equal(0.0, pheromones[0, 0]);
    }

    [Fact]
    public void CalculateTotalDelay_ShouldReturnCorrectSumOfDelays()
    {
        // Arrange
        double[,] customDelays = new double[,]
        {
            { 0, 15, 0 },
            { 0, 0, 25 },
            { 0, 0, 0 }
        };
        SetStaticField("numRouters", 3);
        SetStaticField("delays", customDelays);
        List<int> path = new List<int> { 0, 1, 2 };

        // Act
        double totalDelay = (double)InvokeStaticMethod("CalculateTotalDelay", path);

        // Assert
        Assert.Equal(40.0, totalDelay);
    }

    [Fact]
    public void EvaporatePheromones_ShouldReducePheromonesAndRespectFloorLimit()
    {
        // Arrange
        SetStaticField("numRouters", 2);
        double[,] customPheromones = new double[,]
        {
            { 1.0, 0.00005 },
            { 0.5, 1.0 }
        };
        SetStaticField("pheromones", customPheromones);
        SetStaticField("rho", 0.1); // 10% parowania

        // Act
        InvokeStaticMethod("EvaporatePheromones");

        // Assert
        double[,] pheromones = (double[,])GetStaticField("pheromones");
        
        // 1.0 * (1 - 0.1) = 0.9
        Assert.Equal(0.9, pheromones[0, 0], 5);
        // 0.5 * (1 - 0.1) = 0.45
        Assert.Equal(0.45, pheromones[1, 0], 5);
        // 0.00005 po parowaniu spadłoby poniżej progu, więc powinno zostać ustawione na 0.0001
        Assert.Equal(0.0001, pheromones[0, 1], 5);
    }

    [Fact]
    public void UpdatePheromones_ShouldIncreasePheromonesSymmetrically()
    {
        // Arrange
        SetStaticField("numRouters", 3);
        double[,] customPheromones = new double[,]
        {
            { 1.0, 1.0, 1.0 },
            { 1.0, 1.0, 1.0 },
            { 1.0, 1.0, 1.0 }
        };
        SetStaticField("pheromones", customPheromones);
        SetStaticField("Q", 100.0);

        List<List<int>> paths = new List<List<int>> { new List<int> { 0, 1, 2 } };
        List<double> delaysList = new List<double> { 50.0 }; // DeltaTau = 100 / 50 = 2.0

        // Act
        InvokeStaticMethod("UpdatePheromones", paths, delaysList);

        // Assert
        double[,] pheromones = (double[,])GetStaticField("pheromones");
        
        // Połączenie 0 -> 1 oraz 1 -> 0 powinno wzrosnąć o 2.0 (z 1.0 do 3.0)
        Assert.Equal(3.0, pheromones[0, 1]);
        Assert.Equal(3.0, pheromones[1, 0]);

        // Połączenie 1 -> 2 oraz 2 -> 1 powinno wzrosnąć o 2.0 (z 1.0 do 3.0)
        Assert.Equal(3.0, pheromones[1, 2]);
        Assert.Equal(3.0, pheromones[2, 1]);

        // Połączenie 0 -> 2 nie było w ścieżce, powinno zostać bez zmian (1.0)
        Assert.Equal(1.0, pheromones[0, 2]);
    }

    [Fact]
    public void BuildPathForAnt_ShouldReturnValidPath_WhenRouteExists()
    {
        // Arrange
        double[,] customDelays = new double[,]
        {
            { 0, 10, 0 },
            { 0, 0, 10 },
            { 0, 0, 0 }
        };
        SetStaticField("numRouters", 3);
        SetStaticField("sourceRouter", 0);
        SetStaticField("destRouter", 2);
        SetStaticField("delays", customDelays);
        
        double[,] customPheromones = new double[,]
        {
            { 0, 1.0, 0 },
            { 0, 0, 1.0 },
            { 0, 0, 0 }
        };
        SetStaticField("pheromones", customPheromones);

        // Act
        List<int> path = (List<int>)InvokeStaticMethod("BuildPathForAnt");

        // Assert
        Assert.NotNull(path);
        Assert.Equal(3, path.Count);
        Assert.Equal(0, path[0]);
        Assert.Equal(1, path[1]);
        Assert.Equal(2, path[2]);
    }

    [Fact]
    public void BuildPathForAnt_ShouldReturnNull_WhenAntGetsStuckInDeadEnd()
    {
        // Arrange
        double[,] customDelays = new double[,]
        {
            { 0, 10, 0 },
            { 0, 0, 0 }, // Router 1 to ślepy zaułek, brak połączenia do Routera 2
            { 0, 0, 0 }
        };
        SetStaticField("numRouters", 3);
        SetStaticField("sourceRouter", 0);
        SetStaticField("destRouter", 2);
        SetStaticField("delays", customDelays);

        double[,] customPheromones = new double[,]
        {
            { 0, 1.0, 0 },
            { 0, 0, 0 },
            { 0, 0, 0 }
        };
        SetStaticField("pheromones", customPheromones);

        // Act
        List<int> path = (List<int>)InvokeStaticMethod("BuildPathForAnt");

        // Assert
        Assert.Null(path);
    }
}