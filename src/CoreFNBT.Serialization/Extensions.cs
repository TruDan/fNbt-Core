using System;
using System.Diagnostics;

namespace fNbt.Serialization
{
    internal static class Extensions
    {
	    public static StackFrame GetFrame(this StackTrace trace, int frame)
	    {
		    var frames = trace.GetFrames();
			if (frame > frames.Length) throw new IndexOutOfRangeException();
		    return frames[frame];
	    }
	}
}
