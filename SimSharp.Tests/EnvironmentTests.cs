﻿#region License Information
/* SimSharp - A .NET port of SimPy, discrete event simulation framework
Copyright (C) 2014  Heuristic and Evolutionary Algorithms Laboratory (HEAL)

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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SimSharp.Tests {
  [TestClass]
  public class EnvironmentTests {

    private static IEnumerable<Event> AProcess(Environment env, List<string> log) {
      while (env.Now < new DateTime(1970, 1, 1, 0, 2, 0)) {
        log.Add(env.Now.ToString("mm"));
        yield return env.Timeout(TimeSpan.FromMinutes(1));
      }
    }

    [TestMethod]
    public void test_event_queue_empty() {
      /*The simulation should stop if there are no more events, that means, no
        more active process.*/
      var log = new List<string>();
      var env = new Environment();
      env.Process(AProcess(env, log));
      env.Run(TimeSpan.FromMinutes(10));
      Assert.IsTrue(log.SequenceEqual(new[] { "00", "01" }));
    }

    [TestMethod]
    public void test_run_negative_until() {
      /*Test passing a negative time to run.*/
      var env = new Environment();
      var errorThrown = false;
      try {
        env.Run(new DateTime(1969, 12, 30));
      } catch (InvalidOperationException) {
        errorThrown = true;
      }
      Assert.IsTrue(errorThrown);
    }

    [TestMethod]
    public void test_run_resume() {
      /* Stopped simulation can be resumed. */
      var env = new Environment();
      var events = new List<Event>() {
        env.Timeout(TimeSpan.FromMinutes(5)),
        env.Timeout(TimeSpan.FromMinutes(10)),
        env.Timeout(TimeSpan.FromMinutes(15))
      };

      Assert.AreEqual(env.Now, new DateTime(1970, 1, 1));
      Assert.IsFalse(events.Any(x => x.IsProcessed));

      env.Run(TimeSpan.FromMinutes(10));
      Assert.AreEqual(env.Now, new DateTime(1970, 1, 1, 0, 10, 0));
      Assert.IsTrue(events[0].IsProcessed);
      Assert.IsFalse(events[1].IsProcessed);
      Assert.IsFalse(events[2].IsProcessed);

      env.Run(TimeSpan.FromMinutes(5));
      Assert.AreEqual(env.Now, new DateTime(1970, 1, 1, 0, 15, 0));
      Assert.IsTrue(events[0].IsProcessed);
      Assert.IsTrue(events[1].IsProcessed);
      Assert.IsFalse(events[2].IsProcessed);

      env.Run();
      Assert.AreEqual(env.Now, new DateTime(1970, 1, 1, 0, 15, 0));
      Assert.IsTrue(events[0].IsProcessed);
      Assert.IsTrue(events[1].IsProcessed);
      Assert.IsTrue(events[2].IsProcessed);
    }

    [TestMethod]
    public void test_run_until_value() {
      /* Anything that can be converted to a float is a valid until value. */
      var env = new Environment();
      env.Run(new DateTime(1980, 9, 16, 18, 45, 0));
      Assert.AreEqual(env.Now, new DateTime(1980, 9, 16, 18, 45, 0));
    }
  }
}