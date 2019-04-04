﻿//-----------------------------------------------------------------------
// This file is part of MicroCoin - The first hungarian cryptocurrency
// Copyright (c) 2018 Peter Nemeth
// ECKeyPair.cs - Copyright (c) 2018 Németh Péter
//-----------------------------------------------------------------------
// MicroCoin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MicroCoin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU General Public License for more details.
//-----------------------------------------------------------------------
// You should have received a copy of the GNU General Public License
// along with MicroCoin. If not, see <http://www.gnu.org/licenses/>.
//-----------------------------------------------------------------------


using MicroCoin.Utils;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MicroCoin.Cryptography
{
    public enum CurveType : ushort
    {
        Empty = 0,
        Secp256K1 = 714,
        Secp384R1 = 715,
        Secp521R1 = 716,
        Sect283K1 = 729
    }

    public class ECKeyPair : IEquatable<ECKeyPair>
    {
        public CurveType CurveType { get; set; } = CurveType.Empty;
        public byte[] D { get; set; }
        public Org.BouncyCastle.Math.BigInteger PrivateKey
        {
            get => D == null ? Org.BouncyCastle.Math.BigInteger.Zero : new Org.BouncyCastle.Math.BigInteger(D);
            set => D = value.ToByteArray();
        }

        public ByteString Name { get; set; }

        private ECParameters? _eCParameters;

        public ECPoint PublicKey
        {
            get;
            set;
        } = new ECPoint()
        {
            X = new byte[0],
            Y = new byte[0]
        };

        public ECParameters ECParameters
        {
            get
            {
                if (_eCParameters != null) return _eCParameters.Value;
                ECCurve curve = ECCurve.CreateFromFriendlyName(CurveType.ToString().ToLower());
                ECParameters parameters = new ECParameters
                {
                    Q = PublicKey
                };
                if (D != null)
                {
                    parameters.D = D;
                }

                parameters.Curve = curve;
                parameters.Validate();
                _eCParameters = parameters;
                return _eCParameters.Value;
            }
        }

        public static async Task<ECKeyPair> ImportAsync(string hex) => await Task.Run(() => Import(hex));
        public static ECKeyPair Import(string hex)
        {
            ECKeyPair keyPair = new ECKeyPair();
            keyPair.CurveType = CurveType.Secp256K1;
            var privKeyInt = new Org.BouncyCastle.Math.BigInteger(+1, (Hash)hex);
            var parameters = SecNamedCurves.GetByName("secp256k1");
            var ecPoint = parameters.G.Multiply(privKeyInt);
            keyPair.D = privKeyInt.ToByteArray();
            ECPoint PublicKey = new ECPoint
            {
                X = ecPoint.Normalize().XCoord.ToBigInteger().ToByteArray(),
                Y = ecPoint.Normalize().YCoord.ToBigInteger().ToByteArray()
            };
            keyPair.PublicKey = PublicKey;
            return keyPair;
        }

        public static implicit operator ECParameters(ECKeyPair keyPair)
        {
            return keyPair.ECParameters;
        }

        public void SaveToStream(Stream s, bool writeLength = true, bool writePrivateKey = false,
            bool writeName = false)
        {
            using (BinaryWriter bw = new BinaryWriter(s, Encoding.ASCII, true))
            {
                ushort xLen = (ushort)PublicKey.X.Length;
                if (PublicKey.X[0] == 0) xLen--;

                ushort yLen = (ushort)PublicKey.Y.Length;
                if (PublicKey.Y[0] == 0) yLen--;

                var len = xLen + yLen + 6;

                if (writeName) Name.SaveToStream(bw);
                if (writeLength) bw.Write((ushort)len);
                bw.Write((ushort)CurveType);
                if (CurveType == CurveType.Empty)
                {
                    bw.Write((ushort)0);
                    bw.Write((ushort)0);
                    return;
                }

                byte[] x = PublicKey.X;
                bw.Write(xLen);
                bw.Write(x, x[0] == 0 ? 1 : 0, x.Length - (x[0] == 0 ? 1 : 0));
                if (CurveType == CurveType.Sect283K1)
                {
                    byte[] b = PublicKey.Y;
                    bw.Write(yLen);
                    bw.Write(b, b[0] == 0 ? 1 : 0, b.Length - (b[0] == 0 ? 1 : 0));
                }
                else
                {
                    byte[] b = PublicKey.Y;
                    bw.Write(yLen);
                    bw.Write(b, b[0] == 0 ? 1 : 0, b.Length - (b[0] == 0 ? 1 : 0));
                }

                if (writePrivateKey)
                {
                    D.SaveToStream(bw);
                }
            }
        }

        public static ECKeyPair CreateNew(string name = "")
        {
            SecureRandom secureRandom = new SecureRandom();
            X9ECParameters curve = SecNamedCurves.GetByName("secp256k1");
            ECDomainParameters domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            ECKeyPairGenerator generator = new ECKeyPairGenerator();
            ECKeyGenerationParameters keygenParams = new ECKeyGenerationParameters(domain, secureRandom);
            generator.Init(keygenParams);
            AsymmetricCipherKeyPair keypair = generator.GenerateKeyPair();
            ECPrivateKeyParameters privParams = (ECPrivateKeyParameters)keypair.Private;
            ECPublicKeyParameters pubParams = (ECPublicKeyParameters)keypair.Public;
            ECKeyPair k = new ECKeyPair
            {
                CurveType = CurveType.Secp256K1,
                PrivateKey = privParams.D,
                Name = name
            };
            k.PublicKey = new ECPoint
            {
                X = pubParams.Q.Normalize().XCoord.ToBigInteger().ToByteArray(),
                Y = pubParams.Q.Normalize().YCoord.ToBigInteger().ToByteArray()
            };
            return k;
        }

        public void LoadFromStream(Stream stream, bool doubleLen = true, bool readPrivateKey = false,
            bool readName = false)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.ASCII, true))
            {
                if (readName) Name = ByteString.ReadFromStream(br);
                if (doubleLen)
                {
                    ushort len = br.ReadUInt16();
                    if (len == 0) return;
                }

                CurveType = (CurveType)br.ReadUInt16();
                ushort xLen = br.ReadUInt16();
                var X = br.ReadBytes(xLen);
                ushort yLen = br.ReadUInt16();
                var Y = br.ReadBytes(yLen);
                PublicKey = new ECPoint() { X = X, Y = Y };
                if (readPrivateKey)
                {
                    D = Hash.ReadFromStream(br);
                }
            }
        }

        public bool Equals(ECKeyPair other)
        {
            if (other == null) return false;
            if (other.CurveType != CurveType) return false;
            if (!PublicKey.X.SequenceEqual(other.PublicKey.X)) return false;
            if (!PublicKey.Y.SequenceEqual(other.PublicKey.Y)) return false;
            return true;
        }

        public void DecriptKey(ByteString password)
        {
            byte[] b = new byte[32];
            var salt = D.Skip(8).Take(8).ToArray();
            SHA256Managed managed = new SHA256Managed();
            managed.Initialize();
            managed.TransformBlock(password, 0, password.Length, b, 0);
            managed.TransformFinalBlock(salt, 0, salt.Length);
            var digest = managed.Hash;
            managed.Dispose();
            managed = new SHA256Managed();
            managed.Initialize();
            managed.TransformBlock(digest, 0, digest.Length, b, 0);
            managed.TransformBlock(password, 0, password.Length, b, 0);
            salt = D.Skip(8).Take(8).ToArray();
            managed.TransformFinalBlock(salt, 0, salt.Length);
            var iv = managed.Hash;
            managed.Dispose();
            RijndaelManaged aesEncryption = new RijndaelManaged
            {
                KeySize = 256,
                BlockSize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
            byte[] encryptedBytes = D.Skip(16).ToArray(); //Crazy Salt...
            aesEncryption.IV = iv.Take(16).ToArray();
            aesEncryption.Key = digest;
            ICryptoTransform decrypto = aesEncryption.CreateDecryptor();
            Hash hash = decrypto.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            ByteString bs = hash;
            Hash h2 = bs.ToString(); // dirty hack
            D = h2;
            aesEncryption.Dispose();
        }

        public override string ToString()
        {
            return $"{Name} - {CurveType}";
        }
    }

}