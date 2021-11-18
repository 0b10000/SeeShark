﻿// Copyright (c) The Vignette Authors
// This file is part of SeeShark.
// SeeShark is licensed under LGPL v3. See LICENSE.LESSER.md for details.

using System;
using System.Diagnostics;
using System.Text;
using static SeeShark.FFmpeg.FFmpegManager;

namespace SeeShark.Example
{
    class Program
    {
        private static Camera? karen;
        static void Main(string[] args)
        {
            Console.CancelKeyPress += (object? _sender, ConsoleCancelEventArgs e) =>
            {
                Console.Error.WriteLine("\n\n");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Oof :(");
                Console.ResetColor();
                karen?.StopCapture();
                karen?.Dispose();
                converter?.Dispose();
            };

            Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");
            Console.WriteLine("Running in {0}-bit mode.", Environment.Is64BitProcess ? "64" : "32");
            Console.WriteLine($"FFmpeg version info: {FFmpegVersion}");

            string devicePath = "";

            Console.WriteLine("Creating camera manager...");
            var manager = new CameraManager();

            if (args.Length < 1)
            {
                while (true)
                {
                    Console.WriteLine("\nDevices available:");
                    for (int i = 0; i < manager.Devices.Count; i++)
                    {
                        Console.WriteLine($"#{i} | {manager.Devices[i].Path} ({manager.Devices[i].Name})");
                    }

                    Console.WriteLine("\nChoose a camera by index: ");
                    if (int.TryParse(Console.ReadLine(), out int index) && index < manager.Devices.Count && index >= 0)
                    {
                        devicePath = manager.Devices[index].Path;
                        break;
                    }
                }
            }
            else
            {
                devicePath = args[0];
            }

            Console.WriteLine("\nCreating camera...");
            karen = manager.GetCamera(devicePath);
            karen.NewFrameHandler += OnNewFrame;

            Console.WriteLine("Start the decoding process...");

            Console.WriteLine("Press Space or P to play/pause the camera.");
            Console.WriteLine("Press Enter or Q or Escape to exit the program.");
            var loop = true;
            while (loop)
            {
                Console.WriteLine("\x1b[2K\rCamera is {0}", karen.IsPlaying ? "Playing" : "Paused");
                var cki = Console.ReadKey(true);
                switch (cki.Key)
                {
                    case ConsoleKey.P:
                    case ConsoleKey.Spacebar:
                        if (karen.IsPlaying)
                            karen.StopCapture();
                        else
                            karen.StartCapture();
                        break;

                    case ConsoleKey.Q:
                    case ConsoleKey.Enter:
                    case ConsoleKey.Escape:
                        karen.StopCapture();
                        karen.Dispose();
                        loop = false;
                        converter?.Dispose();
                        break;
                }
            }

            Console.WriteLine("\n\nDid you SeeShark? :)");
        }

        static uint frameCount = 0;
        private static FrameConverter? converter;
        private static readonly Stopwatch watch = new Stopwatch();
        private static readonly StringBuilder builder = new StringBuilder();
        private static float fps = 0;
        public static void OnNewFrame(object? _sender, FrameEventArgs e)
        {
            var frame = e.Frame;
            if (converter == null || Console.WindowWidth != converter.SrcWidth ||
                Console.WindowHeight != converter.SrcHeight)
            {
                converter?.Dispose();
                converter = new FrameConverter(frame.Width, frame.Height, frame.PixelFormat,
                    Console.WindowWidth, Console.WindowHeight, PixelFormat.Gray8);
            }
            else if (e.Status != FFmpeg.DecodeStatus.NewFrame)
            {
                return;
            }

            Frame outputFrame = converter.Convert(frame);
            char[] chars = " `'.,-~:;<>\"^=+*!?|\\/(){}[]#&$@".ToCharArray();

            builder.Clear();
            Console.SetCursorPosition(0, 0);
            int length = outputFrame.Width * outputFrame.Height;
            for (int i = 0; i < length; i++)
                builder.Append(chars[map(outputFrame.RawData[i], 0, 255, 0, chars.Length - 1)]);

            Console.Write(builder.ToString());

            if (frameCount == 10)
            {
                fps = frameCount * 1000f / watch.ElapsedMilliseconds;
                Console.Title = $"{outputFrame.Width}x{outputFrame.Height}@{fps:#.##}fps";
                frameCount = 0;
                watch.Restart();
            }
            else if (frameCount == 0)
            {
                watch.Start();
            }

            frameCount++;
            Console.Out.Flush();
        }

        static int map(int x, int in_min, int in_max, int out_min, int out_max)
        => (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }
}
