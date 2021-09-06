﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using watchtower.Models.Census;
using watchtower.Services.Census;
using watchtower.Services.Db;

namespace watchtower.Services.Hosted.Startup {

    /// <summary>
    /// Runs once at startup, populating the wt_facility table. If an exception occurs, honu can run, so it's just logged
    /// </summary>
    public class FacilityPopulatorStartupService : IHostedService {

        private readonly ILogger<FacilityPopulatorStartupService> _Logger;
        private readonly IFacilityCollection _FacilityCollection;
        private readonly IFacilityDbStore _FacilityDb;

        public FacilityPopulatorStartupService(ILogger<FacilityPopulatorStartupService> logger,
            IFacilityCollection facCollection, IFacilityDbStore facDb) {

            _Logger = logger;

            _FacilityCollection = facCollection ?? throw new ArgumentNullException(nameof(facCollection));
            _FacilityDb = facDb ?? throw new ArgumentNullException(nameof(facDb));
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            try {
                Stopwatch timer = Stopwatch.StartNew();

                List<PsFacility> facilities = await _FacilityCollection.GetAll();
                List<PsFacility> dbFacs = await _FacilityDb.GetAll();

                _Logger.LogInformation($"Census has {facilities.Count} facilities, DB has {dbFacs.Count} facilities");

                foreach (PsFacility fac in facilities) {
                    await _FacilityDb.Upsert(fac.FacilityID, fac);
                }

                _Logger.LogDebug($"Finished in {timer.ElapsedMilliseconds}ms");
            } catch (Exception ex) {
                _Logger.LogError(ex, "Failed to populate facility table");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }
}
