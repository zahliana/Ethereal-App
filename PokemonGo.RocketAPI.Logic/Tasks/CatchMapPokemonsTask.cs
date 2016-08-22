﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PokemonGo.RocketAPI.Logic.Utils;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Pokemon;
using POGOProtos.Networking.Responses;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

namespace PokemonGo.RocketAPI.Logic.Tasks
{
    public class CatchMapPokemonsTask
    {
        public static async Task Execute()
        {
            if (!Logic._client.Settings.CatchPokemon)
                return;

            var pokemons = await GetNearbyPokemons();
            if (pokemons == null || !pokemons.Any())
                return;

            Logger.Write($"Found {pokemons.Count} catchable Pokemon", LogLevel.Debug);
            foreach (var pokemon in pokemons)
            {
                if (Logic._client.Settings.UsePokemonDoNotCatchList && Logic._client.Settings.PokemonsToNotCatch.Contains(pokemon.PokemonId))
                {
                    Logger.Write($"Ignore Pokemon - {pokemon.PokemonId} - is on ToNotCatch List", LogLevel.Debug);
                    continue;
                }

                var encounter = await Logic._client.Encounter.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnPointId);

                if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
                    await CatchPokemonTask.Execute(encounter, pokemon);
                else
                    Logger.Write($"Encounter problem: {encounter.Status}", LogLevel.Warning);
            }

            if (Logic._client.Settings.EvolvePokemon || Logic._client.Settings.EvolvePokemonAboveIV) await EvolvePokemonTask.Execute();
            if (Logic._client.Settings.TransferPokemon) await TransferPokemonTask.Execute();
        }

        private static async Task<List<MapPokemon>> GetNearbyPokemons()
        {
            var mapObjects = await Logic._client.Map.GetMapObjects();

            var pokemons = mapObjects.Item1.MapCells.SelectMany(i => i.CatchablePokemons)
                .OrderBy(
                    i =>
                        LocationUtils.CalculateDistanceInMeters(Logic._client.CurrentLatitude,
                            Logic._client.CurrentLongitude,
                            i.Latitude, i.Longitude)).ToList();

            return pokemons;
        }

    }
}
