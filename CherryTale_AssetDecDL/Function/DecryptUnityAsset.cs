using System;

public static class DecryptUnityAsset
{
    public static byte[] ChangeVersion(byte[] data)
    {
        // 2018.3.5f1.
        byte[] verion_fake = { 0x32, 0x30, 0x31, 0x38, 0x2E, 0x33, 0x2E, 0x35, 0x66, 0x31, 0x00 };
        // 2020.3.41f1
        byte[] verion = { 0x32, 0x30, 0x32, 0x30, 0x2E, 0x33, 0x2E, 0x34, 0x31, 0x66, 0x31 };

        long data_size = data.Length;
        int datahead_size = 0x1000;
        byte[] datahead = new byte[datahead_size];
        Array.Copy(data, datahead, datahead_size);

        ArrayReplaceAll(datahead, verion_fake, verion);

        byte[] newdata = new byte[data_size];
        Array.Copy(datahead, 0, newdata, 0, datahead_size);
        Array.Copy(data, datahead_size, newdata, datahead_size, data_size - datahead_size);

        return newdata;
    }

    private static void ArrayReplaceAll(byte[] source, byte[] oldBytes, byte[] newBytes)
    {
        for (int i = 0; i < source.Length - oldBytes.Length + 1; i++)
        {
            bool match = true;
            for (int j = 0; j < oldBytes.Length; j++)
            {
                if (source[i + j] != oldBytes[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                Array.Copy(newBytes, 0, source, i, newBytes.Length);
            }
        }
    }

    public static byte[] ChangeIdx(byte[] iFileData)
    {
        int v4 = 0, v8, v11, v12, v19;
        byte v21;
        int[] v7 = new int[3] { 0x3FB, 0xD99, 0x197C };

        while (true)
        {
            v11 = v7.Length;
            if (v4 >= v11)
                break;

            // v9 = v7->m_Items; v12 = *(_DWORD *)&v9[4 * v4];
            v12 = v7[v4];
            v8 = iFileData.Length;
            if (v8 > v12)
            {
                v21 = iFileData[v12];
                iFileData[v12] = iFileData[v8 - v12];
                v19 = iFileData.Length - v12;
                iFileData[v19] = v21;
            }
            v4++;
        }

        return iFileData;
    }
}
