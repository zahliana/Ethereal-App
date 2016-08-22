    #region

using PokemonGo.RocketAPI.Enums;
using System.Collections.Generic;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;

#endregion


namespace PokemonGo.RocketAPI
{
    public interface ISettings
    {
        AuthType AuthType { get; }
        string PTCPassword { get; }
        string PTCUsername { get; }
        string GoogleEmail { get; }
        string GooglePassword { get; }
        double DefaultLatitude { get; }
        double DefaultLongitude { get; }
        double DefaultAltitude { get; }
        bool UseGPXPathing { get; }
        string GPXFile { get; }
        bool GPXIgnorePokestops { get; }
        double WalkSpeedKPH { get; }
        int MaxTravelDistanceInMeters { get; }
        bool TeleportInsteadOfWalk { get; }

        bool UsePokemonDoNotCatchList { get; }
        bool UsePokemonToNotTransferList { get; }
        bool UsePokemonToEvolveList { get; }
        bool CatchPokemon { get; }
        bool CatchIncensePokemon { get; }
        bool CatchLuredPokemon { get; }
        bool EvolvePokemon { get; }
        bool EvolvePokemonAboveIV { get; }
        float EvolvePokemonAboveIVValue { get; }
        int EvolveCandyAmountToEvolve { get; }

        bool TransferPokemon { get; }
        bool TransferPokemonKeepIfCanEvolve { get; }
        bool UseTransferPokemonKeepAboveCP { get; }
        int TransferPokemonKeepAboveCPValue { get; }
        bool UseTransferPokemonKeepAboveIV { get; }
        float TransferPokemonKeepAboveIVValue { get; }
        int TransferPokemonKeepAmountHighestCP { get; }
        int TransferPokemonKeepAmountHighestIV { get; }

        bool HatchEggs { get; }
        bool UseOnlyBasicIncubator { get; }
        bool UseLuckyEggs { get; }
        bool PrioritizeIVOverCP { get; }
        int ExportPokemonToCsvEveryMinutes { get; }
        bool DebugMode { get; }
        string DevicePackageName { get; }

        ICollection<KeyValuePair<ItemId, int>> ItemRecycleFilter(IEnumerable<ItemData> myItems);
        ICollection<PokemonId> PokemonsToEvolve { get; }
        ICollection<PokemonId> PokemonsToNotTransfer { get; }
        ICollection<PokemonId> PokemonsToNotCatch { get; }
    }
}