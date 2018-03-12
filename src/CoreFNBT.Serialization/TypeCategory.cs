using System;

namespace fNbt.Serialization {
    [Flags]
    internal enum TypeCategory {
        NotSupported = 0,
        Primitive = 1,
        ConvertiblePrimitive = 2,
        Enum = 4,
        MappedToPrimitive = Primitive | ConvertiblePrimitive | Enum,
        String = 8,
        ByteArray = 16,
        IntArray = 32,
		LongArray = 64,
        DirectlyMapped = MappedToPrimitive | String | ByteArray | IntArray | LongArray,
        Array = 128,
        IList = 256,
        MappedToList = Array | IList,
        IDictionary = 512,
        ConvertibleByProperties = 1024,
        MappedToCompound = ConvertibleByProperties | IDictionary,
        Mapped = DirectlyMapped | MappedToList | MappedToCompound,
        NbtTag = 2048,
        INbtSerializable = 4096
    }
}