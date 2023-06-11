using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using watchtower.Code.ExtensionMethods;
using watchtower.Models.Census;

namespace watchtower.Services.Db {

    public class CharacterDirectiveObjectiveDbStore {

        private readonly ILogger<CharacterDirectiveObjectiveDbStore> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IDataReader<CharacterDirectiveObjective> _Reader;

        public CharacterDirectiveObjectiveDbStore(ILogger<CharacterDirectiveObjectiveDbStore> logger,
            IDbHelper helper, IDataReader<CharacterDirectiveObjective> reader) {

            _Logger = logger;
            _DbHelper = helper;
            _Reader = reader;
        }

        /// <summary>
        ///     Get all <see cref="CharacterDirectiveObjective"/>s for a single character
        /// </summary>
        /// <param name="charID">ID of the character</param>
        /// <returns>
        ///     A <see cref="List{T}"/> of <see cref="CharacterDirectiveObjective"/>s with
        ///     <see cref="CharacterDirectiveObjective.CharacterID"/> of <paramref name="charID"/>
        /// </returns>
        public async Task<List<CharacterDirectiveObjective>> GetByCharacterID(string charID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.CHARACTER);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT *
                    FROM character_directive_objective
                    WHERE character_id = @CharacterID;
            ");

            cmd.AddParameter("CharacterID", charID);

            List<CharacterDirectiveObjective> dirs = await _Reader.ReadList(cmd);
            await conn.CloseAsync();

            return dirs;
        }

        /// <summary>
        ///     Upsert a single <see cref="CharacterDirectiveObjective"/>
        /// </summary>
        /// <param name="charID">ID of the character this info is being updated for</param>
        /// <param name="dir">Parameters used to upsert</param>
        public async Task Upsert(string charID, CharacterDirectiveObjective dir) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.CHARACTER);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO character_directive_objective (
                    character_id, directive_id, objective_id, objective_group_id, status, state_data
                ) VALUES (
                    @CharacterID, @DirectiveID, @ObjectiveID, @ObjectiveGroupID, @Status, @StateData
                ) ON CONFLICT (character_id, directive_id) DO UPDATE
                    SET objective_id = @ObjectiveID,
                        objective_group_id = @ObjectiveGroupID,
                        status = @Status,
                        state_data = @StateData
            ");

            cmd.AddParameter("CharacterID", charID);
            cmd.AddParameter("DirectiveID", dir.DirectiveID);
            cmd.AddParameter("ObjectiveID", dir.ObjectiveID);
            cmd.AddParameter("ObjectiveGroupID", dir.ObjectiveGroupID);
            cmd.AddParameter("Status", dir.Status);
            cmd.AddParameter("StateData", dir.StateData);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }

}