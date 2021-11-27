// <copyright file="Program.cs" company="Vatsan Madhavan">
// Copyright (c) Vatsan Madhavan. All rights reserved.
// </copyright>

using FontInstaller;

foreach (var fontFace in FontUtils.FontFaces)
{
    Console.WriteLine(fontFace);
}