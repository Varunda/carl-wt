﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using watchtower.Models;
using watchtower.Models.Db;
using watchtower.Services.Census;
using watchtower.Services.Db;

namespace watchtower.Services.Hosted {

    public class HostedRealtimeMapStateCollector : BackgroundService {

        private readonly ILogger<HostedRealtimeMapStateCollector> _Logger;
        private readonly IServiceHealthMonitor _ServiceHealth;

        private readonly RealtimeMapStateCollection _RealtimeMapCensus;
        private readonly RealtimeMapStateDbStore _RealtimeMapDb;

        private const string SERVICE_NAME = "realtime_map_state_collector";

        public HostedRealtimeMapStateCollector(ILogger<HostedRealtimeMapStateCollector> logger,
            RealtimeMapStateCollection realtimeMapCensus, RealtimeMapStateDbStore realtimeMapDb, IServiceHealthMonitor serviceHealth) {

            _Logger = logger;
            _ServiceHealth = serviceHealth;

            _RealtimeMapCensus = realtimeMapCensus;
            _RealtimeMapDb = realtimeMapDb;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _Logger.LogInformation($"starting");

            while (stoppingToken.IsCancellationRequested == false) {
                try {
                    ServiceHealthEntry health = _ServiceHealth.Get(SERVICE_NAME) ?? new() {
                        Name = SERVICE_NAME,
                    };

                    if (health.Enabled == false) {
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    _Logger.LogDebug($"getting map state and saving to db");
                    Stopwatch timer = Stopwatch.StartNew();

                    List<RealtimeMapState> states = await _RealtimeMapCensus.GetAll(stoppingToken);
                    long censusMs = timer.ElapsedMilliseconds; timer.Restart();

                    foreach (RealtimeMapState state in states) {
                        await _RealtimeMapDb.Insert(state, stoppingToken);
                    }
                    long dbMs = timer.ElapsedMilliseconds;

                    _Logger.LogInformation($"saved realtime map info in {censusMs + dbMs}ms. [Census={censusMs}ms] [DB={dbMs}ms]");

                    health.LastRan = DateTime.UtcNow;
                    health.RunDuration = censusMs + dbMs;
                    health.Message = $"saved {states.Count} entries from realtime map state";
                    _ServiceHealth.Set(SERVICE_NAME, health);

                } catch (Exception ex) when (stoppingToken.IsCancellationRequested == false) {
                    _Logger.LogError(ex, $"failed to get realtime map state");
                } catch (Exception) when (stoppingToken.IsCancellationRequested == true) {
                    _Logger.LogInformation($"stopping service");
                }

                await Task.Delay(60 * 1000, stoppingToken);
            }
        }

    }
}
