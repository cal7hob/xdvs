using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

namespace CodeStage.AntiCheat.ObscuredTypes
{
    public class CachedObscuredString : ObscuredString
    {
        private string cachedValue;

        protected CachedObscuredString(string value) : base(InternalEncrypt(value))
        {
            RenewCache();
        }

        public void RenewCache()
        {
            cachedValue = InternalDecrypt();
        }

        public static implicit operator CachedObscuredString(string value)
        {
            if (value == null)
            {
                return null;
            }

            CachedObscuredString obscured = new CachedObscuredString(value);
            if (Detectors.ObscuredCheatingDetector.isRunning)
            {
                obscured.fakeValue = value;
            }
            return obscured;
        }

        public static implicit operator string(CachedObscuredString value)
        {
            return value.cachedValue;
        }
    }
}