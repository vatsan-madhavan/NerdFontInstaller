﻿// <copyright file="Program.cs" company="Vatsan Madhavan">
// Copyright (c) Vatsan Madhavan. All rights reserved.
// </copyright>

#pragma warning disable SA1200 // Using directives should be placed correctly
using FontInstaller;

foreach (var fontFace in FontUtils.FontFaces)
{
    Console.WriteLine(fontFace);
}

var ttfs = new string[]
{
    @"C:\Users\Vatsan\Downloads\CascadiaCode (1)\Caskaydia Cove Nerd Font Complete Mono Windows Compatible.ttf",
    @"C:\Users\Vatsan\Downloads\CascadiaCode (1)\Caskaydia Cove Nerd Font Complete Mono.ttf",
    @"C:\Users\Vatsan\Downloads\CascadiaCode (1)\Caskaydia Cove Nerd Font Complete Windows Compatible.ttf",
    @"C:\Users\Vatsan\Downloads\CascadiaCode (1)\Caskaydia Cove Nerd Font Complete.ttf",
};

foreach (var ttf in ttfs)
{
    Console.WriteLine($"Following Font faces are found in {ttf}:");
    foreach (var faceName in FontUtils.GetFontFaceNames(ttf))
    {
        Console.WriteLine($"\t{faceName}");
    }
}