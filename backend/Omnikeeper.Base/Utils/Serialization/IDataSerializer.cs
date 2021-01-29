﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Omnikeeper.Base.Utils.Serialization
{
    public interface IDataSerializer
    {
        byte[] ToByteArray(object obj);
        T? FromByteArray<T>(byte[] byteArray) where T : class;
    }
}