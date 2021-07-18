#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenRA.Mods.Cnc.FileFormats;

namespace OpenRA.Mods.Cnc.FileSystem
{
	public enum PackageHashType { Classic, CRC32 }

	public class PackageEntry
	{
		public const int Size = 12;
		public readonly uint Hash;
		public readonly uint Offset;
		public readonly uint Length;

		public PackageEntry(uint hash, uint offset, uint length)
		{
			Hash = hash;
			Offset = offset;
			Length = length;
		}

		public PackageEntry(Stream s)
		{
			Hash = s.ReadUInt32();
			Offset = s.ReadUInt32();
			Length = s.ReadUInt32();
		}

		public void Write(BinaryWriter w)
		{
			w.Write(Hash);
			w.Write(Offset);
			w.Write(Length);
		}

		public override string ToString()
		{
			if (names.TryGetValue(Hash, out var filename))
				return $"{filename} - offset 0x{Offset:x8} - length 0x{Length:x8}";
			else
				return $"0x{Hash:x8} - offset 0x{Offset:x8} - length 0x{Length:x8}";
		}

		public static uint HashFilename(string name, PackageHashType type)
		{
			switch (type)
			{
				case PackageHashType.Classic:
					{
						name = name.ToUpperInvariant();
						if (name.Length % 4 != 0)
							name = name.PadRight(name.Length + (4 - name.Length % 4), '\0');

						var result = 0u;
						var data = Encoding.ASCII.GetBytes(name);
						var i = 0;
						while (i < data.Length)
						{
							var next = (uint)(data[i++] | data[i++] << 8 | data[i++] << 16 | data[i++] << 24);
							result = ((result << 1) | (result >> 31)) + next;
						}

						return result;
					}

				case PackageHashType.CRC32:
					{
						name = name.ToUpperInvariant();
						var l = name.Length;
						var a = l >> 2;
						if ((l & 3) != 0)
						{
							name += (char)(l - (a << 2));
							var i = 3 - (l & 3);
							while (i-- != 0)
								name += name[a << 2];
						}

						return CRC32.Calculate(Encoding.ASCII.GetBytes(name));
					}

				default: throw new NotImplementedException($"Unknown hash type `{type}`");
			}
		}

		static Dictionary<uint, string> names = new Dictionary<uint, string>();

		public static void AddStandardName(string s)
		{
			var hash = HashFilename(s, PackageHashType.Classic); // RA1 and TD
			names.Add(hash, s);
			var crcHash = HashFilename(s, PackageHashType.CRC32); // TS
			names.Add(crcHash, s);
		}
	}
}
