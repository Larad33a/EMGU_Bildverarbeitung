
Imports System.Runtime.InteropServices
Imports System.Threading 'Für Tasks zu beennden

Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Structure
Imports Emgu.CV.UI
Imports Emgu.CV.Util

Public Class UmwandlungClass
    Public Enum ValueTyp
        Min
        Max
        Midel
    End Enum
    Public Enum AreaTyp
        Area_3X3 = 1
        Area_5X5 = 2
        Area_7X7 = 3
        Area_15x15 = 7
    End Enum

    Public Shared Function Mat16SInMat32F(InputMat As Mat) As Mat
        Dim OutputMat As New Mat(InputMat.Height, InputMat.Width, DepthType.Cv32S, 1)
        For x = 0 To InputMat.Width - 1
            For y = 0 To InputMat.Height - 1
                Dim Val = GetInt16Value(InputMat, y, x)
                SetInt32Value(OutputMat, y, x, Val)
            Next
        Next
        Return OutputMat
    End Function

    Public Shared Function GetInt16Value(ByVal mat As Mat, ByVal row As Integer, ByVal col As Integer) As Int16
        Dim value(0) As Int16
        Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 1)
        Return value(0)
    End Function

    Public Shared Function GetInt32Value(ByVal mat As Mat, ByVal row As Integer, ByVal col As Integer) As Int32
        Dim value(0) As Int32
        Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 1)
        Return value(0)
    End Function

    Public Shared Function GetSingleValue(ByVal mat As Mat, ByVal row As Integer, ByVal col As Integer) As Single
        Dim value(0) As Single
        Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 1)
        Return value(0)
    End Function

    Public Shared Sub SetInt32Value(ByVal mat As Mat, ByVal row As Integer, ByVal col As Integer, ByVal value As Int32)
        Dim target = {value}
        Marshal.Copy(target, 0, mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, 1)
    End Sub

    Public Shared Sub SetByteValues(ByVal mat As Mat, ByVal row As Integer, ByVal col As Integer, ByVal value As Byte)
        Dim target = {value}
        Marshal.Copy(target, 0, mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, 1)
    End Sub

    Public Shared Sub SetByteValues(ByVal mat As Mat, ByVal row As Integer, ByVal col As Integer, values As Byte())
        Marshal.Copy(values, 0,
                     destination:=mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize,
                     length:=values.Count)
    End Sub

    Public Shared Function GetByteValue(ByVal mat As Mat, ByVal row As Integer, ByVal col As Integer) As Byte()
        Dim value(2) As Byte
        Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 3)
        Return value
    End Function

    Public Shared Function GetAreaValue_Byte(ByVal mat As Mat, ByVal row As Integer, ByVal col As Integer, Area As AreaTyp, typ As ValueTyp) As Byte()
        Dim num As Int32 = 0
        Dim mymid As Int32() = {0, 0, 0}
        Dim mymin As Byte() = {255, 255, 255}
        Dim mymax As Byte() = {0, 0, 0}
        Dim wert As Byte() = {0, 0, 0}
        For i = row - Area To row + Area
            For j = col - Area To col + Area
                If (i >= 0 And i <= mat.Rows) And (j >= 0 And j <= mat.Cols) Then
                    Try
                        wert = GetByteValue(mat, i, j)
                        num += 1
                        'Max
                        If wert(0) + wert(1) + wert(2) > mymax(0) + mymax(1) + mymax(2) Then
                            mymax = wert
                        End If
                        'Min
                        If wert(0) + wert(1) + wert(2) < mymin(0) + mymin(1) + mymin(2) Then
                            mymin = wert
                        End If
                        'Middel
                        mymid(0) = mymid(0) + wert(0)
                        mymid(1) = mymid(1) + wert(1)
                        mymid(2) = mymid(2) + wert(2)
                    Catch ex As Exception
                        num -= 1
                    End Try
                End If
            Next
        Next
        Dim result As Byte() = {0, 0, 0}
        result(0) = CByte(Math.Round(mymid(0) / num))
        result(1) = CByte(Math.Round(mymid(1) / num))
        result(2) = CByte(Math.Round(mymid(2) / num))
        If typ = ValueTyp.Max Then
            Return mymax
        End If
        If typ = ValueTyp.Min Then
            Return mymin
        End If
        If typ = ValueTyp.Midel Then
            Return result
        End If
        Return wert
    End Function
    Public Shared Function GetAreaValue_Singel(ByVal mat As Mat, ByVal row As Integer, ByVal col As Integer, Area As AreaTyp, typ As ValueTyp) As Single
        Dim num As Int32 = 0
        Dim mymid As Double = 0
        Dim mymin As Single = Single.MaxValue
        Dim mymax As Single = Single.MinValue
        Dim wert As Single = 0
        For i = row - Area To row + Area
            For j = col - Area To col + Area
                If (i >= 0 And i <= mat.Rows) And (j >= 0 And j <= mat.Cols) Then
                    Try
                        wert = GetSingleValue(mat, i, j)
                        num += 1
                        'Max
                        If wert > mymax Then
                            mymax = wert
                        End If
                        'Min
                        If wert < mymin Then
                            mymin = wert
                        End If
                        'Middel
                        mymid = mymid + wert
                    Catch ex As Exception
                        num -= 1
                    End Try
                End If
            Next
        Next
        Dim result As Single = CSng(mymid / num)
        If typ = ValueTyp.Max Then
            Return mymax
        End If
        If typ = ValueTyp.Min Then
            Return mymin
        End If
        If typ = ValueTyp.Midel Then
            Return result
        End If
        Return wert
    End Function
    Public Shared Function GetAreaValue_Int16(ByVal mat As Mat, ByVal row As Integer, ByVal col As Integer, Area As AreaTyp, typ As ValueTyp) As Int16
        Dim num As Int32 = 0
        Dim mymid As Int64 = 0
        Dim mymin As Int16 = Int16.MaxValue
        Dim mymax As Int16 = Int16.MinValue
        Dim wert As Int16 = 0
        For i = row - Area To row + Area
            For j = col - Area To col + Area
                If (i >= 0 And i <= mat.Rows) And (j >= 0 And j <= mat.Cols) Then
                    Try
                        wert = GetInt16Value(mat, i, j)
                        num += 1
                        'Max
                        If wert > mymax Then
                            mymax = wert
                        End If
                        'Min
                        If wert < mymin Then
                            mymin = wert
                        End If
                        'Middel
                        mymid = mymid + wert
                    Catch ex As Exception
                        num -= 1
                    End Try
                End If
            Next
        Next
        Dim result As Int16 = CShort(Math.Round(mymid / num))
        If typ = ValueTyp.Max Then
            Return mymax
        End If
        If typ = ValueTyp.Min Then
            Return mymin
        End If
        If typ = ValueTyp.Midel Then
            Return result
        End If
        Return wert
    End Function
    Public Shared Function GetAreaValue_Int32(ByVal mat As Mat, ByVal row As Integer, ByVal col As Integer, Area As AreaTyp, typ As ValueTyp) As Int32
        Dim num As Int32 = 0
        Dim mymid As Int64 = 0
        Dim mymin As Int32 = Int32.MaxValue
        Dim mymax As Int32 = Int32.MinValue
        Dim wert As Int32 = 0
        For i = row - Area To row + Area
            For j = col - Area To col + Area
                If (i >= 0 And i <= mat.Rows) And (j >= 0 And j <= mat.Cols) Then
                    Try
                        wert = GetInt32Value(mat, i, j)
                        num += 1
                        'Max
                        If wert > mymax Then
                            mymax = wert
                        End If
                        'Min
                        If wert < mymin Then
                            mymin = wert
                        End If
                        'Middel
                        mymid = mymid + wert
                    Catch ex As Exception
                        num -= 1
                    End Try
                End If
            Next
        Next
        Dim result As Int32 = CInt(Math.Round(mymid / num))
        If typ = ValueTyp.Max Then
            Return mymax
        End If
        If typ = ValueTyp.Min Then
            Return mymin
        End If
        If typ = ValueTyp.Midel Then
            Return result
        End If
        Return wert
    End Function
End Class
