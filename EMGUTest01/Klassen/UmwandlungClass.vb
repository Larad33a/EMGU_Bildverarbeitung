
Imports System.Runtime.InteropServices
Imports System.Threading 'Für Tasks zu beennden

Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Structure
Imports Emgu.CV.UI
Imports Emgu.CV.Util

Public Class UmwandlungClass
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
End Class
