//---------------------------------------------------------------------------------
// Copyright (c) March 2020, devMobile Software
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.FieldGateway.Client
{
   using System;
   using System.Text;
   using System.Threading;

   using Radios.RF24;

   using Meadow;
   using Meadow.Devices;
   using Meadow.Foundation.Leds;
   using Meadow.Foundation.Sensors.Atmospheric;
   using Meadow.Hardware;
   using Meadow.Peripherals.Leds;

   public class MeadowClient : App<F7Micro, MeadowClient>
   {
      private const string BaseStationAddress = "Base1";
      private const string DeviceAddress = "WLAB1";
      private const byte nRF24Channel = 15;
      private RF24 Radio = new RF24();
      private readonly TimeSpan periodTime = new TimeSpan(0, 0, 60);
      private readonly Sht31D sensor;
      private readonly ILed Led;

      public MeadowClient()
      {
         Led = new Led(Device, Device.Pins.OnboardLedGreen);

         try
         {
            sensor = new Sht31D(Device.CreateI2cBus());

            var config = new Meadow.Hardware.SpiClockConfiguration(
                           2000,
                           SpiClockConfiguration.Mode.Mode0);

            ISpiBus spiBus = Device.CreateSpiBus(
               Device.Pins.SCK,
               Device.Pins.MOSI,
               Device.Pins.MISO, config);

            Radio.OnDataReceived += Radio_OnDataReceived;
            Radio.OnTransmitFailed += Radio_OnTransmitFailed;
            Radio.OnTransmitSuccess += Radio_OnTransmitSuccess;

            Radio.Initialize(Device, spiBus, Device.Pins.D09, Device.Pins.D10, Device.Pins.D11);
            Radio.Address = Encoding.UTF8.GetBytes(DeviceAddress);

            Radio.Channel = nRF24Channel;
            Radio.PowerLevel = PowerLevel.Low;
            Radio.DataRate = DataRate.DR250Kbps;
            Radio.IsEnabled = true;

            Radio.IsAutoAcknowledge = true;
            Radio.IsDyanmicAcknowledge = false;
            Radio.IsDynamicPayload = true;

            Console.WriteLine($"Address: {Encoding.UTF8.GetString(Radio.Address)}");
            Console.WriteLine($"PowerLevel: {Radio.PowerLevel}");
            Console.WriteLine($"IsAutoAcknowledge: {Radio.IsAutoAcknowledge}");
            Console.WriteLine($"Channel: {Radio.Channel}");
            Console.WriteLine($"DataRate: {Radio.DataRate}");
            Console.WriteLine($"IsDynamicAcknowledge: {Radio.IsDyanmicAcknowledge}");
            Console.WriteLine($"IsDynamicPayload: {Radio.IsDynamicPayload}");
            Console.WriteLine($"IsEnabled: {Radio.IsEnabled}");
            Console.WriteLine($"Frequency: {Radio.Frequency}");
            Console.WriteLine($"IsInitialized: {Radio.IsInitialized}");
            Console.WriteLine($"IsPowered: {Radio.IsPowered}");
         }
         catch (Exception ex)
         {
            Console.WriteLine(ex.Message);
         }

         while (true)
         {
            sensor.Update();

            Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss}-TX T:{sensor.Temperature:0.0}C H:{sensor.Humidity:0}%");

            Led.IsOn = true;

            string values = "T " + sensor.Temperature.ToString("F1") + ",H " + sensor.Humidity.ToString("F0");

            // Stuff the 2 byte header ( payload type & deviceIdentifierLength ) + deviceIdentifier into payload
            byte[] payload = new byte[1 + Radio.Address.Length + values.Length];
            payload[0] = (byte)((1 << 4) | Radio.Address.Length);
            Array.Copy(Radio.Address, 0, payload, 1, Radio.Address.Length);
            Encoding.UTF8.GetBytes(values, 0, values.Length, payload, Radio.Address.Length + 1);

            Radio.SendTo(Encoding.UTF8.GetBytes(BaseStationAddress), payload);

            Thread.Sleep(periodTime);
         }
      }

      private void Radio_OnDataReceived(byte[] data)
      {
         // Display as Unicode
         string unicodeText = Encoding.UTF8.GetString(data);
         Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss}-RX Unicode Length {0} Unicode Length {1} Unicode text {2}", data.Length, unicodeText.Length, unicodeText);

         // display as hex
         Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss}-RX Hex Length {data.Length} Payload {BitConverter.ToString(data)}");
      }

      private void Radio_OnTransmitSuccess()
      {
         Led.IsOn = false;

         Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss}-TX Succeeded!");
      }

      private void Radio_OnTransmitFailed()
      {
         Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss}-TX failed!");
      }
   }
}
