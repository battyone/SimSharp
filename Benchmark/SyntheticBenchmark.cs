﻿#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2018  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;

namespace SimSharp.Benchmarks {
  public class SyntheticBenchmark {
    private static long perf;
    private static Simulation env;
    private static void Timeout(object sender, ElapsedEventArgs e) {
      env.StopAsync();
    }

    public static int Run(SyntheticOptions opts) {
      if (!Stopwatch.IsHighResolution) {
        Console.WriteLine("The Stopwatch class on this machine is not based on a high resolution counter.");
        return 1;
      }
      var time = opts.Time;
      var repetitions = opts.Repetitions;
      var CPU_FRQ = (long)Math.Round(opts.CpuFreq * 1E9);

      Console.WriteLine();
      Console.WriteLine("== Starting Benchmark: {0} repetitions for {1:##,###}ms each. CPU clock frequency is set to {2:##,###}Hz ==", repetitions, (int)(time * 1000), CPU_FRQ);
      Console.WriteLine();
      var frqMod = CPU_FRQ / (double)Stopwatch.Frequency;

      var sumTime = 0.0;
      var sumPerf = 0.0;
      foreach (var n in new[] { 1, 10, 100, 1000, 10000 }) {
        for (var r = 0; r < repetitions; r++) {
          env = new Simulation();
          var clk = new Timer((int)(time * 1000));
          clk.Elapsed += Timeout;
          clk.Start();
          sumTime += Benchmark1(env, n);
          sumPerf += perf;
          clk.Stop();
          clk.Elapsed -= Timeout;
        }
        sumTime /= repetitions;
        sumPerf /= repetitions;
        Console.WriteLine("Benchmark 1 (n = {0,6:##,0}): {1,7:0,0} clock cycles ({2,10:0,0} entities / s)", n, frqMod * sumTime / sumPerf, Stopwatch.Frequency * sumPerf / sumTime);
      }

      sumTime = 0.0;
      sumPerf = 0.0;
      for (var r = 0; r < repetitions; r++) {
        env = new Simulation();
        var clk = new Timer((int)(time * 1000));
        clk.Elapsed += Timeout;
        clk.Start();
        sumTime += Benchmark2(env);
        sumPerf += perf;
        clk.Stop();
        clk.Elapsed -= Timeout;
      }
      sumTime /= repetitions;
      sumPerf /= repetitions;
      Console.WriteLine("Benchmark 2: {0,20:0,0} clock cycles ({1,10:0,0} entities / s)", frqMod * sumTime / sumPerf, Stopwatch.Frequency * sumPerf / sumTime);

      sumTime = 0.0;
      sumPerf = 0.0;
      for (var r = 0; r < repetitions; r++) {
        env = new Simulation();
        var clk = new Timer((int)(time * 1000));
        clk.Elapsed += Timeout;
        clk.Start();
        sumTime += Benchmark3(env);
        sumPerf += perf;
        clk.Stop();
        clk.Elapsed -= Timeout;
      }
      sumTime /= repetitions;
      sumPerf /= repetitions;
      Console.WriteLine("Benchmark 3: {0,20:0,0} clock cycles ({1,10:0,0} entities / s)", frqMod * sumTime / sumPerf, Stopwatch.Frequency * sumPerf / sumTime);
      Console.WriteLine();
      Console.WriteLine("== Finished Benchmark ==");
      Console.ReadLine();

      return 0;
    }

    /// <summary>
    /// This method will benchmark Sim#'s performance with respect to the list
    /// of future events. A large number of processes that exist in the system
    /// stress tests the performance of operations on the event queue.
    /// </summary>
    /// <param name="n">The number of concurrent processes.</param>
    static long Benchmark1(Simulation env, int n) {
      perf = 0;
      for (var i = 0; i < n; i++) {
        env.Process(Benchmark1Proc(env, n));
      }
      var watch = Stopwatch.StartNew();
      env.Run();
      watch.Stop();
      return watch.ElapsedTicks;
    }

    static IEnumerable<Event> Benchmark1Proc(Simulation env, int n) {
      while (true) {
        yield return env.TimeoutUniform(TimeSpan.Zero, TimeSpan.FromSeconds(2 * n));
        perf++;
      }
    }

    /// <summary>
    /// This method will benchmark Sim#'s performance with respect to creation
    /// of entities. In SimPy and also Sim# the equivalence of an entity is a
    /// process. This stress tests the performance of creating processes.
    /// </summary>
    static long Benchmark2(Simulation env) {
      perf = 0;
      env.Process(Benchmark2Source(env));
      var watch = Stopwatch.StartNew();
      env.Run();
      watch.Stop();
      return watch.ElapsedTicks;
    }

    static IEnumerable<Event> Benchmark2Source(Simulation env) {
      while (true) {
        yield return env.Process(Benchmark2Sink(env));
        perf++;
      }
    }

    static IEnumerable<Event> Benchmark2Sink(Simulation env) {
      yield break;
    }

    /// <summary>
    /// This method will benchmark Sim#'s performance with respect to
    /// seizing and releasing resources, a common task in DES models.
    /// </summary>
    static long Benchmark3(Simulation env) {
      perf = 0;
      var res = new Resource(env, capacity: 1);
      env.Process(Benchmark3Proc(env, res));
      env.Process(Benchmark3Proc(env, res));
      var watch = Stopwatch.StartNew();
      env.Run();
      watch.Stop();
      return watch.ElapsedTicks;
    }

    static IEnumerable<Event> Benchmark3Proc(Simulation env, Resource resource) {
      while (true) {
        var req = resource.Request();
        yield return req;
        yield return resource.Release(req);
        perf++;
      }
    }
  }
}
