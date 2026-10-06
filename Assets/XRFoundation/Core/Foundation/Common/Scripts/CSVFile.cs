using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class CSVFile
{
    List<string[]> _data = new List<string[]>();

    /// <summary>
    /// Data
    /// </summary>
    public string[][] Data
    {
        get { return _data.ToArray(); }
    }

    int _rowCount = 0;

    /// <summary>
    /// Row count
    /// </summary>
    public int RowCount
    {
        get { return _rowCount; }
    }

    int _colCount = 0;

    /// <summary>
    /// Column count
    /// </summary>
    public int ColCount
    {
        get { return _colCount; }
    }

    /// <summary>
    /// Generate from data
    /// </summary>
    /// <param name="data"></param>
    public CSVFile(byte[] data, Encoding encoding)
    {
        var content = encoding.GetString(data);

        string[] rows = content.Split(new string[] { "\r\n" }, StringSplitOptions.None);
        MyDebugTool.LogError("rows:" + rows.Length);

        ParseRows(rows);
    }

    public CSVFile(string path)
    {
        System.Text.Encoding encoding = GetEncoding(path);
        string[] rows = File.ReadAllLines(path, encoding);
        ParseRows(rows);
    }

    void ParseRows(string[] rows)
    {
        for (int i = 0; i < rows.Length; i++)
        {
            if (string.IsNullOrEmpty(rows[i]))
            {
                continue;
            }
            else
            {
                var cols = GetCols(rows[i]);
                if (null != cols)
                {
                    _data.Add(cols.ToArray());
                }
            }

        }

        _rowCount = _data.Count;
        if (_rowCount > 0)
        {
            _colCount = _data[0].Length;
        }
    }

    /// <summary>
    /// Get the table value
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public string GetValue(int row, int col)
    {
        return _data[row][col];
    }

    public string[] GetValue(int row)
    {

        string[] tmpStr = new string[ColCount];
        tmpStr[0] = _data[row][0];
        for (int i = 1; i < ColCount; i++)
        {
            tmpStr[i] = (_data[row][i]);
        }
        return tmpStr;
    }

    /// <summary>
    /// Split a row into columns
    /// </summary>
    /// <param name="rowContent"></param>
    /// <returns></returns>
    List<string> GetCols(string rowContent)
    {
        //Quotation mark
        const char QUOTATION_MARKS = '"';
        //Comma
        const char COMMA = ',';

        List<string> cols = new List<string>();
        //Delimiter position (also the first character index of a column)
        int splitMark = 0;
        int charIdx = 0;
        bool isSpecial = false;
        while (charIdx < rowContent.Length)
        {
            char c = rowContent[charIdx];
            int nextIdx = charIdx + 1;

            if (charIdx == splitMark)
            {
                if (c == QUOTATION_MARKS)
                {
                    isSpecial = true;
                }
                else
                {
                    isSpecial = false;
                    if (nextIdx == rowContent.Length)
                    {
                        //Terminator
                        string colContent = rowContent.Substring(splitMark);
                        cols.Add(colContent);
                        break;
                    }
                }
            }
            else
            {
                if (isSpecial)
                {
                    //Handle strings containing special characters
                    if (c == QUOTATION_MARKS)
                    {
                        if (nextIdx == rowContent.Length)
                        {
                            //Terminator
                            string colContent = rowContent.Substring(splitMark + 1, charIdx - splitMark - 1);
                            colContent = colContent.Replace("\"\"", "\"");
                            cols.Add(colContent);
                            //Skip the next quotation mark
                            charIdx++;
                        }
                        else
                        {
                            char nextC = rowContent[nextIdx];
                            if (nextC == QUOTATION_MARKS)
                            {
                                //Skip the double quote
                                charIdx++;
                            }
                            else if (nextC == COMMA)
                            {
                                //Delimiter
                                string colContent = rowContent.Substring(splitMark + 1, charIdx - splitMark - 1);
                                colContent = colContent.Replace("\"\"", "\"");
                                cols.Add(colContent);
                                charIdx++;
                                splitMark = nextIdx + 1;
                            }
                        }
                    }
                }
                else
                {
                    //Handle ordinary string content
                    if (c == COMMA)
                    {
                        //Delimiter
                        string colContent = rowContent.Substring(splitMark, charIdx - splitMark);
                        cols.Add(colContent);
                        splitMark = charIdx + 1;
                    }

                    if (nextIdx == rowContent.Length)
                    {
                        //Terminator
                        string colContent = rowContent.Substring(splitMark);
                        cols.Add(colContent);
                        break;
                    }
                }
            }

            charIdx++;
        }

        return cols;
    }

    System.Text.Encoding GetEncoding(string FILE_NAME)
    {
        FileStream fs = new FileStream(FILE_NAME, FileMode.Open, FileAccess.Read);
        System.Text.Encoding r = GetEncoding(fs);
        fs.Close();
        return r;
    }


    System.Text.Encoding GetEncoding(FileStream fs)
    {
        BinaryReader r = new BinaryReader(fs, System.Text.Encoding.Default);
        byte[] ss = r.ReadBytes(3);
        r.Close();
        //Encoding type; for example, Coding = Encoding.ASCII
        if (ss[0] >= 0xEF)
        {
            if (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF)
            {
                return System.Text.Encoding.UTF8;
            }
            else if (ss[0] == 0xFE && ss[1] == 0xFF)
            {
                return System.Text.Encoding.BigEndianUnicode;
            }
            else if (ss[0] == 0xFF && ss[1] == 0xFE)
            {
                return System.Text.Encoding.Unicode;
            }
            else
            {
                return System.Text.Encoding.Default;
            }
        }
        else
        {
            return System.Text.Encoding.Default;
        }
    }
}

