using Google.Protobuf;
using PokemonGo.RocketAPI.Enums;
using POGOProtos.Networking;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Logic.Utils;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace PokemonGo.RocketAPI.Helpers
{
    public class RequestBuilder
    {
        private readonly string _authToken;
        private readonly AuthType _authType;
        private readonly double _latitude;
        private readonly double _longitude;
        private readonly double _altitude;
        private readonly AuthTicket _authTicket; //Added
        private readonly DateTime _startTime = DateTime.UtcNow; //Added
        private ulong _nextRequestId;
        static private readonly Stopwatch _internalWatch = new Stopwatch();
        private readonly ISettings settings; //Added

        public RequestBuilder(string authToken, AuthType authType, double latitude, double longitude, double altitude,
            AuthTicket authTicket = null)
        {
            _authToken = authToken;
            _authType = authType;
            _latitude = latitude;
            _longitude = longitude;
            _altitude = altitude;
            _authTicket = authTicket;
            this.settings = settings; //Added
            if (!_internalWatch.IsRunning)
                _internalWatch.Start();
        }

        private Unknown6 GenerateSignature(IEnumerable<IMessage> requests)
        {
            var ticketBytes = _authTicket.ToByteArray(); //Added
            var sig = new Signature()
            {
                LocationHash1 = Utils.GenerateLocation1(ticketBytes, _latitude, _longitude, _altitude),
                LocationHash2 = Utils.GenerateLocation2(_latitude, _longitude, _altitude),
                SensorInfo = new Signature.Types.SensorInfo()
                {
                    AccelNormalizedZ = GenRandom(9.8),
                    AccelNormalizedX = GenRandom(0.02),
                    AccelNormalizedY = GenRandom(0.3),
                    TimestampSnapshot = (ulong)_internalWatch.ElapsedMilliseconds - 230,
                    MagnetometerX = GenRandom(0.12271042913198471),
                    MagnetometerY = GenRandom(-0.015570580959320068),
                    MagnetometerZ = GenRandom(0.010850906372070313),
                    AngleNormalizedX = GenRandom(17.950439453125),
                    AngleNormalizedY = GenRandom(-23.36273193359375),
                    AngleNormalizedZ = GenRandom(-48.8250732421875),
                    AccelRawX = GenRandom(-0.0120010357350111),
                    AccelRawY = GenRandom(-0.04214850440621376),
                    AccelRawZ = GenRandom(0.94571763277053833),
                    GyroscopeRawX = GenRandom(7.62939453125e-005),
                    GyroscopeRawY = GenRandom(-0.00054931640625),
                    GyroscopeRawZ = GenRandom(0.0024566650390625),
                    AccelerometerAxes = 3
                },
                DeviceInfo = new Signature.Types.DeviceInfo()
                {
                    DeviceId = Client.DeviceId,
                    AndroidBoardName = Client.AndroidBoardName,
                    AndroidBootloader = Client.AndroidBootloader,
                    DeviceBrand = Client.DeviceBrand,
                    DeviceModel = Client.DeviceModel,
                    DeviceModelIdentifier = Client.DeviceModelIdentifier,
                    DeviceModelBoot = Client.DeviceModelBoot,
                    HardwareManufacturer = Client.HardwareManufacturer,
                    HardwareModel = Client.HardwareModel,
                    FirmwareBrand = Client.FirmwareBrand,
                    FirmwareTags = Client.FirmwareTags,
                    FirmwareType = Client.FirmwareType,
                    FirmwareFingerprint = Client.FirmwareFingerprint
                }
            };

            sig.LocationFix.Add(new Signature.Types.LocationFix()
            {
                Provider = "network",

                //Unk4 = 120,
                Latitude = (float)_latitude,
                Longitude = (float)_longitude,
                Altitude = (float)_altitude,
                TimestampSinceStart = (ulong)_internalWatch.ElapsedMilliseconds - 200,
                Floor = 3,
                LocationType = 1
            });

            //Compute 10
            var x = new System.Data.HashFunction.xxHash(32, 0x1B845238);
            var firstHash = BitConverter.ToUInt32(x.ComputeHash(_authTicket.ToByteArray()), 0);
            x = new System.Data.HashFunction.xxHash(32, firstHash);
            var locationBytes = BitConverter.GetBytes(_latitude).Reverse()
                .Concat(BitConverter.GetBytes(_longitude).Reverse())
                .Concat(BitConverter.GetBytes(_altitude).Reverse()).ToArray();
            sig.LocationHash1 = BitConverter.ToUInt32(x.ComputeHash(locationBytes), 0);
            
            //Compute 20
            x = new System.Data.HashFunction.xxHash(32, 0x1B845238);
            sig.LocationHash2 = BitConverter.ToUInt32(x.ComputeHash(locationBytes), 0);
            
            //Compute 24
            x = new System.Data.HashFunction.xxHash(64, 0x1B845238);
            var seed = BitConverter.ToUInt64(x.ComputeHash(_authTicket.ToByteArray()), 0);
            x = new System.Data.HashFunction.xxHash(64, seed);

            foreach (var req in requests)
                sig.RequestHash.Add(BitConverter.ToUInt64(x.ComputeHash(req.ToByteArray()), 0));

            Unknown6 val = new Unknown6();
            val.RequestType = 6;
            val.Unknown2 = new Unknown6.Types.Unknown2();
            val.Unknown2.Unknown1 = ByteString.CopyFrom(Encrypt(sig.ToByteArray()));
            
            foreach (var request in requests)
                sig.RequestHash.Add(BitConverter.ToUInt64(x.ComputeHash(request.ToByteArray()), 0));

            sig.Unk22 = ByteString.CopyFrom(new byte[16] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F });
            sig.Unk25 = BitConverter.ToUInt32(new System.Data.HashFunction.xxHash(64, 0x88533787).ComputeHash(System.Text.Encoding.ASCII.GetBytes("\"b8fa9757195897aae92c53dbcf8a60fb3d86d745\"")), 0);
            
            return val;
        }

        [DllImport("encrypt.dll", EntryPoint = "encrypt", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        static extern private void EncryptNative(IntPtr arr, int length, byte[] iv, int ivsize, IntPtr output, out int outputSize);
        [DllImport("kernel32.dll", EntryPoint = "RtlFillMemory", SetLastError = false)]
        static extern void FillMemory(IntPtr destination, uint length, byte fill);

        private static byte[] GetURandom(int size)
        {
            var rng = new RNGCryptoServiceProvider();
            var buffer = new byte[size];
            rng.GetBytes(buffer);
            return buffer;
        }
        private byte[] Encrypt(byte[] bytes)
        {
            var outputLength = 32 + bytes.Length + (256 - (bytes.Length % 256));
            var ptr = Marshal.AllocHGlobal(outputLength);
            var ptrOutput = Marshal.AllocHGlobal(outputLength);
            FillMemory(ptr, (uint)outputLength, 0);
            FillMemory(ptrOutput, (uint)outputLength, 0);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);

            var iv = GetURandom(32);
            var iv_ptr = Marshal.AllocHGlobal(iv.Length);
            Marshal.Copy(iv, 0, iv_ptr, iv.Length);

            try
            {
                var outputSize = outputLength;
                EncryptNative(ptr, bytes.Length, iv, iv.Length, ptrOutput, out outputSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            var output = new byte[outputLength];
            Marshal.Copy(ptrOutput, output, 0, outputLength);
            return output;
        }

  

        public RequestEnvelope GetRequestEnvelope(params Request[] customRequests)
        {
            var e = new RequestEnvelope
            {
                StatusCode = 2, //1

                RequestId = 1469378659230941192, //3
                Requests = { customRequests }, //4

                //Unknown6 = , //6
                Latitude = _latitude, //7
                Longitude = _longitude, //8
                Altitude = _altitude, //9
                AuthTicket = _authTicket, //11
                Unknown12 = 989 //12
            };
            e.Unknown6.Add(GenerateSignature(customRequests));
            return e;
        }

        public RequestEnvelope GetInitialRequestEnvelope(params Request[] customRequests)
        {
            var e = new RequestEnvelope
            {
                StatusCode = 2, //1

                RequestId = 1469378659230941192, //3
                Requests = { customRequests }, //4

                //Unknown6 = , //6
                Latitude = _latitude, //7
                Longitude = _longitude, //8
                Altitude = _altitude, //9
                AuthInfo = new POGOProtos.Networking.Envelopes.RequestEnvelope.Types.AuthInfo
                {
                    Provider = _authType == AuthType.Google ? "google" : "ptc",
                    Token = new POGOProtos.Networking.Envelopes.RequestEnvelope.Types.AuthInfo.Types.JWT
                    {
                        Contents = _authToken,
                        Unknown2 = 14
                    }
                }, //10
                Unknown12 = 989 //12
            };
            return e;
        }

        public RequestEnvelope GetRequestEnvelope(RequestType type, IMessage message)
        {
            return GetRequestEnvelope(new Request()
            {
                RequestType = type,
                RequestMessage = message.ToByteString()
            });

        }

        private static readonly Random RandomDevice = new Random();
        public static double GenRandom(double num)
        {
            var randomFactor = 0.3f;
            var randomMin = (num * (1 - randomFactor));
            var randomMax = (num * (1 + randomFactor));
            var randomizedDelay = RandomDevice.NextDouble() * (randomMax - randomMin) + randomMin; ;
            return randomizedDelay; ;
        }

        public static double GenRandom(double min, double max)
        {
            return RandomDevice.NextDouble() * (min - min) + min;
        }

        public static string GetDeviceId()
        {
            byte[] DeviceUUID = new byte[8];
            Random random = new Random();
            random.NextBytes(DeviceUUID);
            return BitConverter.ToString(DeviceUUID).Replace("-", "");
        }
        public static void SetDevice(ISettings settings)
        {
            // Do some post-load logic to determine what device info to be using - if 'custom' is set we just take what's in the file without question
            if (!settings.DevicePackageName.Equals("random", StringComparison.InvariantCultureIgnoreCase))
            {
                // User requested a specific device package, check to see if it exists and if so, set it up - otherwise fall-back to random package
                SetDevInfoByKey(settings.DevicePackageName);
            }
            else if (settings.DevicePackageName.Equals("random", StringComparison.InvariantCultureIgnoreCase))
            {
                // Random is set, so pick a random device package and set it up - it will get saved to disk below and re-used in subsequent sessions
                Random rnd = new Random();
                var rndIdx = rnd.Next(0, DeviceInfoHelper.DeviceInfoSets.Keys.Count - 1);
                var devicePackageName = DeviceInfoHelper.DeviceInfoSets.Keys.ToArray()[rndIdx];
                SetDevInfoByKey(devicePackageName);
            }
        }
        private static void SetDevInfoByKey(string devicePackageName)
        {
            if (DeviceInfoHelper.DeviceInfoSets.ContainsKey(devicePackageName))
            {
                Client.AndroidBoardName = DeviceInfoHelper.DeviceInfoSets[devicePackageName]["AndroidBoardName"];
                Client.AndroidBootloader = DeviceInfoHelper.DeviceInfoSets[devicePackageName]["AndroidBootloader"];
                Client.DeviceBrand = DeviceInfoHelper.DeviceInfoSets[devicePackageName]["DeviceBrand"];
                //Client.DeviceId = DeviceInfoHelper.DeviceInfoSets[DevicePackageName]["DeviceId"];
                Client.DeviceModel = DeviceInfoHelper.DeviceInfoSets[devicePackageName]["DeviceModel"];
                Client.DeviceModelBoot = DeviceInfoHelper.DeviceInfoSets[devicePackageName]["DeviceModelBoot"];
                Client.DeviceModelIdentifier = DeviceInfoHelper.DeviceInfoSets[devicePackageName]["DeviceModelIdentifier"];
                Client.FirmwareBrand = DeviceInfoHelper.DeviceInfoSets[devicePackageName]["FirmwareBrand"];
                Client.FirmwareFingerprint = DeviceInfoHelper.DeviceInfoSets[devicePackageName]["FirmwareFingerprint"];
                Client.FirmwareTags = DeviceInfoHelper.DeviceInfoSets[devicePackageName]["FirmwareTags"];
                Client.FirmwareType = DeviceInfoHelper.DeviceInfoSets[devicePackageName]["FirmwareType"];
                Client.HardwareManufacturer = DeviceInfoHelper.DeviceInfoSets[devicePackageName]["HardwareManufacturer"];
                Client.HardwareModel = DeviceInfoHelper.DeviceInfoSets[devicePackageName]["HardwareModel"];
            }
            else
            {
                throw new ArgumentException("Invalid device info package! Check your auth.config file and make sure a valid DevicePackageName is set. For simple use set it to 'random'. If you have a custom device, then set it to 'custom'.");
            }
        }
    }
}