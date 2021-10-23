﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using watchtower.Models.Census;
using watchtower.Models.CharacterViewer.CharacterStats;
using watchtower.Services.CharacterViewer;
using watchtower.Services.Repositories;

namespace watchtower.Controllers.Api {

    [ApiController]
    [Route("/api/")]
    public class CharacterApiController : ControllerBase {

        private readonly ILogger<CharacterApiController> _Logger;

        private readonly ICharacterRepository _CharacterRepository;
        private readonly ICharacterStatGeneratorStore _GeneratorStore;
        private readonly ICharacterHistoryStatRepository _HistoryRepository;

        public CharacterApiController(ILogger<CharacterApiController> logger,
            ICharacterRepository charRepo, ICharacterStatGeneratorStore genStore,
            ICharacterHistoryStatRepository histRepo) {

            _Logger = logger;

            _CharacterRepository = charRepo;
            _GeneratorStore = genStore ?? throw new ArgumentNullException(nameof(genStore));
            _HistoryRepository = histRepo ?? throw new ArgumentNullException(nameof(histRepo));
        }

        [HttpGet("character/{charID}")]
        public async Task<ActionResult<PsCharacter?>> GetByID(string charID) {
            if (charID.All(char.IsDigit) == false) {
                return BadRequest($"{nameof(charID)} was not all digits: '{charID}'");
            }

            PsCharacter? c = await _CharacterRepository.GetByID(charID);
            if (c == null) {
                return NoContent();
            }

            return Ok(c);
        }

        [HttpGet("character/{charID}/extra")]
        public async Task<ActionResult<List<CharacterStatBase>>> GetExtraStats(string charID) {
            PsCharacter? c = await _CharacterRepository.GetByID(charID);
            if (c == null) {
                return NotFound($"{nameof(PsCharacter)} {charID}");
            }

            List<CharacterStatBase> stats = await _GeneratorStore.GenerateAll(charID);

            return Ok(stats);
        }

        [HttpGet("character/{charID}/history_stats")]
        public async Task<ActionResult<List<PsCharacterHistoryStat>>> GetHistoryStats(string charID) {
            PsCharacter? c = await _CharacterRepository.GetByID(charID);
            if (c == null) {
                return NotFound($"{nameof(PsCharacter)} {charID}");
            }

            List<PsCharacterHistoryStat> stats = await _HistoryRepository.GetByCharacterID(charID);

            return Ok(stats);
        }

        [HttpGet("characters/name/{name}")]
        public async Task<ActionResult<List<PsCharacter>>> GetByName(string name) {
            List<PsCharacter> chars = await _CharacterRepository.GetByName(name);

            return Ok(chars);
        }

    }
}
