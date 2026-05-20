using System;
using System.Collections.Generic;
using System.Linq;

class TelecomAntRouting
{
    static int numAnts = 20;
    static int maxIter = 15;        
    static double alpha = 1.0;
    static double beta = 2.0;
    static double rho = 0.1;
    static double Q = 100.0;

    static int numRouters = 6;
    static int sourceRouter = 0;
    static int destRouter = 5;

    static double[,] delays = new double[,]
    {
        // Router: 0   1   2   3   4   5
        /* 0 */ {  0, 10, 50,  0,  0,  0 }, 
        /* 1 */ { 10,  0, 10, 20,  0,  0 }, 
        /* 2 */ { 50, 10,  0,  0, 10,  0 }, 
        /* 3 */ {  0, 20,  0,  0, 10, 30 }, 
        /* 4 */ {  0,  0, 10, 10,  0, 50 }, 
        /* 5 */ {  0,  0,  0, 30, 50,  0 }
    };

    static double[,] pheromones = new double[numRouters, numRouters];

    static void Main()
    {
        InitializePheromones();

        List<int> globalBestPath = null;
        double globalBestDelay = double.MaxValue;

        Console.WriteLine("Symulacja routingu mrówkowego: \n");

        for (int iter = 0; iter < maxIter; iter++)
        {
            List<List<int>> antPaths = new List<List<int>>();
            List<double> antDelays = new List<double>();

            double iterBestDelay = double.MaxValue;
            List<int> iterBestPath = null;

            for (int k = 0; k < numAnts; k++)
            {
                List<int> path = BuildPathForAnt();
                if (path != null)
                {
                    double delay = CalculateTotalDelay(path);
                    antPaths.Add(path);
                    antDelays.Add(delay);

                    if (delay < iterBestDelay)
                    {
                        iterBestDelay = delay;
                        iterBestPath = new List<int>(path);
                    }

                    if (delay < globalBestDelay)
                    {
                        globalBestDelay = delay;
                        globalBestPath = new List<int>(path);
                    }
                }
            }

            Console.WriteLine($"\n[Iteracja {iter + 1}/{maxIter}]");

            if (iterBestPath != null)
            {
                Console.WriteLine($"  -> Najszybsza mrówka w tej iteracji: {string.Join(" -> ", iterBestPath)} (Czas: {iterBestDelay} ms)");
            }
            else
            {
                Console.WriteLine("  -> Wszystkie mrówki utknęły w ślepych zaułkach!");
            }

            EvaporatePheromones();
            UpdatePheromones(antPaths, antDelays);

            if (globalBestPath != null)
            {
                Console.Write("  -> Feromony na globalnie najlepszej trasie: ");
                for (int i = 0; i < globalBestPath.Count - 1; i++)
                {
                    int from = globalBestPath[i];
                    int to = globalBestPath[i + 1];
                    
                    Console.Write($"[{from}-{to}: {Math.Round(pheromones[from, to], 2)}] ");
                }
                Console.WriteLine();
            }
        }

        // WYPISANIE WYNIKU
        Console.WriteLine("\n=================================");
        Console.WriteLine("=== WYNIK OPTYMALIZACJI ===");
        Console.WriteLine("=================================");
        Console.WriteLine($"Najszybsza znaleziona trasa: {string.Join(" -> ", globalBestPath)}");
        Console.WriteLine($"Całkowite opóźnienie: {globalBestDelay} ms");
        Console.ReadLine();
    }

    static void InitializePheromones()
    {
        for (int i = 0; i < numRouters; i++)
            for (int j = 0; j < numRouters; j++)
                if (delays[i, j] > 0)
                    pheromones[i, j] = 1.0;
    }

    static List<int> BuildPathForAnt()
    {
        List<int> path = new List<int>();
        bool[] visited = new bool[numRouters];

        int currentRouter = sourceRouter;
        path.Add(currentRouter);
        visited[currentRouter] = true;

        Random rnd = new Random();

        while (currentRouter != destRouter)
        {
            List<int> neighbors = new List<int>();
            List<double> probabilities = new List<double>();
            double probabilitySum = 0;

            for (int j = 0; j < numRouters; j++)
            {
                if (delays[currentRouter, j] > 0 && !visited[j])
                {
                    neighbors.Add(j);

                    double tau = Math.Pow(pheromones[currentRouter, j], alpha);
                    double eta = Math.Pow(1.0 / delays[currentRouter, j], beta);

                    double p = tau * eta;
                    probabilities.Add(p);
                    probabilitySum += p;
                }
            }

            if (neighbors.Count == 0) return null;

            double r = rnd.NextDouble() * probabilitySum;
            double cumulative = 0;
            int nextRouter = -1;

            for (int i = 0; i < neighbors.Count; i++)
            {
                cumulative += probabilities[i];
                if (r <= cumulative)
                {
                    nextRouter = neighbors[i];
                    break;
                }
            }

            path.Add(nextRouter);
            visited[nextRouter] = true;
            currentRouter = nextRouter;
        }

        return path;
    }

    static double CalculateTotalDelay(List<int> path)
    {
        double total = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            total += delays[path[i], path[i + 1]];
        }
        return total;
    }

    static void EvaporatePheromones()
    {
        for (int i = 0; i < numRouters; i++)
        {
            for (int j = 0; j < numRouters; j++)
            {
                pheromones[i, j] *= (1.0 - rho);
                if (pheromones[i, j] < 0.0001) pheromones[i, j] = 0.0001;
            }
        }
    }

    static void UpdatePheromones(List<List<int>> paths, List<double> delaysList)
    {
        for (int k = 0; k < paths.Count; k++)
        {
            double deltaTau = Q / delaysList[k];
            List<int> path = paths[k];

            for (int i = 0; i < path.Count - 1; i++)
            {
                int from = path[i];
                int to = path[i + 1];

                pheromones[from, to] += deltaTau;
                pheromones[to, from] += deltaTau;
            }
        }
    }
}