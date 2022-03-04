﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using watchtower.Models;
using watchtower.Models.Alert;
using watchtower.Models.Census;
using watchtower.Models.Db;
using watchtower.Services.Db;
using watchtower.Services.Repositories;

namespace watchtower.Controllers.Api {

    [ApiController]
    [Route("/api/alerts")]
    public class AlertApiController : ApiControllerBase {

        private readonly ILogger<AlertApiController> _Logger;

        private readonly AlertDbStore _AlertDb;
        private readonly AlertParticipantDataRepository _ParticipantDataRepository;
        private readonly CharacterRepository _CharacterRepository;
        private readonly OutfitRepository _OutfitRepository;
        private readonly SessionDbStore _SessionDb;

        public AlertApiController(ILogger<AlertApiController> logger,
                AlertParticipantDataRepository participantDataRepository, AlertDbStore alertDb,
                CharacterRepository characterRepository,
                OutfitRepository outfitRepository, SessionDbStore sessionDb) {

            _Logger = logger;

            _ParticipantDataRepository = participantDataRepository;
            _AlertDb = alertDb;
            _CharacterRepository = characterRepository;
            _OutfitRepository = outfitRepository;
            _SessionDb = sessionDb;
        }

        /// <summary>
        ///     Get the alert with the corresponding ID
        /// </summary>
        /// <param name="alertID">ID of the alert</param>
        /// <response code="200">
        ///     The response will contain the <see cref="PsAlert"/> with <see cref="PsAlert.ID"/> of <paramref name="alertID"/>
        /// </response> 
        /// <response code="204">
        ///     No <see cref="PsAlert"/> with <see cref="PsAlert.ID"/> of <paramref name="alertID"/> exists
        /// </response>
        [HttpGet("{alertID}")]
        public async Task<ApiResponse<PsAlert>> GetAlertByID(long alertID) {
            PsAlert? alert = await _AlertDb.GetByID(alertID);

            if (alert == null) {
                return ApiNoContent<PsAlert>();
            }

            return ApiOk(alert);
        }

        /// <summary>
        ///     Get all alerts
        /// </summary>
        /// <response code="200">
        ///     Get all alerts Honu has tracked
        /// </response>
        [HttpGet]
        public async Task<ApiResponse<List<PsAlert>>> GetAll() {
            List<PsAlert> alerts = await _AlertDb.GetAll();

            return ApiOk(alerts);
        }

        /// <summary>
        ///     Get the participant data for an alert, as well as data that would be useful in displaying this data, such as the characters relevant
        /// </summary>
        /// <param name="alertID">ID of the alert</param>
        /// <param name="excludeCharacters">
        ///     If the resulting <see cref="ExpandedAlertParticipants"/> will NOT have <see cref="ExpandedAlertParticipants.Characters"/> fill in. 
        ///     Useful for services that use Honu API, but not the frontend
        /// </param>
        /// <param name="excludeOutfits">
        ///     If the resulting <see cref="ExpandedAlertParticipants"/> will NOT have <see cref="ExpandedAlertParticipants.Outfits"/> fill in. 
        ///     Useful for services that use Honu API, but not the frontend
        /// </param>
        /// <param name="excludeSessions">
        ///     If the resulting <see cref="ExpandedAlertParticipants"/> will NOT have <see cref="ExpandedAlertParticipants.Sessions"/> fill in. 
        ///     Useful for services that use Honu API, but not the frontend
        /// </param>
        /// <response code="200">
        ///     The response will contain the <see cref="ExpandedAlertParticipants"/> for the alert requested, 
        ///     optionally including data that may be useful to the requester. The requester can request to not
        ///     recieve this data, by using <paramref name="excludeCharacters"/> and co
        /// </response>
        /// <response code="400">
        ///     The alert exists, but has not yet finished
        /// </response>
        /// <response code="404">
        ///     No <see cref="PsAlert"/> with <see cref="PsAlert.ID"/> of <paramref name="alertID"/> exists
        /// </response>
        [HttpGet("{alertID}/participants")]
        public async Task<ApiResponse<ExpandedAlertParticipants>> GetParticipants(
                long alertID,
                [FromQuery] bool excludeCharacters = false,
                [FromQuery] bool excludeOutfits = false,
                [FromQuery] bool excludeSessions = false
            ) {

            PsAlert? alert = await _AlertDb.GetByID(alertID);
            if (alert == null) {
                return ApiNotFound<ExpandedAlertParticipants>($"{nameof(PsAlert)} {alertID}");
            }

            DateTime alertEnd = alert.Timestamp + TimeSpan.FromSeconds(alert.Duration);
            if (DateTime.UtcNow < alertEnd) {
                return ApiBadRequest<ExpandedAlertParticipants>($"{nameof(PsAlert)} {alertID} has not finished ({alertEnd:u})");
            }

            ExpandedAlertParticipants block = new ExpandedAlertParticipants();

            List<AlertParticipantDataEntry> entries = await _ParticipantDataRepository.GetByAlert(alert, CancellationToken.None);
            block.Entries = entries;

            if (excludeCharacters == false) {
                List<string> charIDs = entries.Select(iter => iter.CharacterID).Distinct().ToList();
                List<PsCharacter> characters = await _CharacterRepository.GetByIDs(charIDs);
                block.Characters = characters;
            }

            if (excludeOutfits == false) {
                List<string> outfitIDs = entries.Where(iter => iter.OutfitID != null).Select(iter => iter.OutfitID!).Distinct().ToList(); // force is safe
                List<PsOutfit> outfits = await _OutfitRepository.GetByIDs(outfitIDs);
                block.Outfits = outfits;
            }

            if (excludeSessions == false) {
                List<Session> sessions = await _SessionDb.GetByRange(alert.Timestamp, alert.Timestamp + TimeSpan.FromSeconds(alert.Duration));

                HashSet<string> validCharacterIds = new HashSet<string>();
                foreach (AlertParticipantDataEntry entry in entries) {
                    validCharacterIds.Add(entry.CharacterID);
                }

                foreach (Session session in sessions) {
                    if (validCharacterIds.Contains(session.CharacterID) == false) {
                        continue;
                    }

                    block.Sessions.Add(session);
                }
            }

            return ApiOk(block);
        }

    }
}
