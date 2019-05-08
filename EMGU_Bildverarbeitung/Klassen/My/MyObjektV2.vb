Imports Emgu.CV
Imports Emgu.CV.Structure
Imports Emgu.CV.Util

Public Class MyObjektV2
    '--------------------------------------------------------------------------------------------------------------------------------------------------------
    'Property
    '--------------------------------------------------------------------------------------------------------------------------------------------------------
    'ID------------------------------------------------------
    Private _ID As Int32
    Public Property ID() As Int32
        Get
            Return _ID
        End Get
        Set(ByVal value As Int32)
            _ID = value
        End Set
    End Property

    'Max & Min-----------------------------------------------
    Private _Max_X As MyPoint = Nothing
    Public ReadOnly Property Max_X() As MyPoint
        Get
            Return _Max_X
        End Get
    End Property
    Private _Min_X As MyPoint = Nothing
    Public ReadOnly Property Min_X() As MyPoint
        Get
            Return _Min_X
        End Get
    End Property
    Private _Max_Y As MyPoint = Nothing
    Public ReadOnly Property Max_Y() As MyPoint
        Get
            Return _Max_Y
        End Get
    End Property
    Private _Min_Y As MyPoint = Nothing
    Public ReadOnly Property Min_Y() As MyPoint
        Get
            Return _Min_Y
        End Get
    End Property
    Private _Max_Z As MyPoint = Nothing
    Public ReadOnly Property Max_Z() As MyPoint
        Get
            Return _Max_Z
        End Get
    End Property
    Private _Min_Z As MyPoint = Nothing
    Public ReadOnly Property Min_Z() As MyPoint
        Get
            Return _Min_Z
        End Get
    End Property

    'Color---------------------------------------------------
    Private _Color As Byte()
    Public Property Color() As Byte()
        Get
            Return _Color
        End Get
        Set(ByVal value As Byte())
            _Color = value
        End Set
    End Property


    '--------------------------------------------------------------------------------------------------------------------------------------------------------
    'Variablen
    '--------------------------------------------------------------------------------------------------------------------------------------------------------
    'Reverenz------------------------------------------------
    'XY
    Private _ReverenzenXY As New VectorOfPoint
    'Z
    Private _ReverenzenZ As New VectorOfInt

    'MinAreaRec----------------------------------------------
    Private _MinAreaRec As New RotatedRect

    '--------------------------------------------------------------------------------------------------------------------------------------------------------
    'Konstruktor
    '--------------------------------------------------------------------------------------------------------------------------------------------------------
    Sub New()

    End Sub
    Sub New(id As Int32)
        _ID = id
    End Sub
    Sub New(id As Int32, color As Byte())
        _ID = id
        _Color = color
    End Sub


    '--------------------------------------------------------------------------------------------------------------------------------------------------------
    'Subs
    '--------------------------------------------------------------------------------------------------------------------------------------------------------
    'Privat--------------------------------------------------
    Private Sub CheckMinMax(point As MyPoint)
        'x
        If _Max_X Is Nothing Then
            _Max_X = point
        Else
            If point.X > _Max_X.X Then
                _Max_X = point
            End If
        End If
        If _Min_X Is Nothing Then
            _Min_X = point
        Else
            If point.X < _Min_X.X Then
                _Min_X = point
            End If
        End If
        'y
        If _Max_Y Is Nothing Then
            _Max_Y = point
        Else
            If point.Y > _Max_Y.Y Then
                _Max_Y = point
            End If
        End If
        If _Min_Y Is Nothing Then
            _Min_Y = point
        Else
            If point.Y < _Min_Y.Y Then
                _Min_Y = point
            End If
        End If
        'z
        If _Max_Z Is Nothing Then
            _Max_Z = point
        Else
            If point.Z > _Max_Z.Z Then
                _Max_Z = point
            End If
        End If
        If _Min_Z Is Nothing Then
            _Min_Z = point
        Else
            If point.Z < _Min_Z.Z Then
                _Min_Z = point
            End If
        End If
    End Sub


    'Public--------------------------------------------------
    'AddRef
    Public Sub Add_Ref(x As Int32, y As Int32, z As Int32)
        Dim tmpPoint As New Point(x, y)
        Dim tmpPointArray(0) As Point
        tmpPointArray(0) = tmpPoint
        _ReverenzenXY.Push(tmpPointArray)
        Dim tmpIntArray(0) As Int32
        tmpIntArray(0) = z
        _ReverenzenZ.Push(tmpIntArray)
        CheckMinMax(New MyPoint(x, y, z))
    End Sub
    Public Sub Add_Ref(point As MyPoint)
        Dim tmpPoint As New Point(point.X, point.Y)
        Dim tmpPointArray(0) As Point
        tmpPointArray(0) = tmpPoint
        _ReverenzenXY.Push(tmpPointArray)
        Dim tmpIntArray(0) As Int32
        tmpIntArray(0) = point.Z
        _ReverenzenZ.Push(tmpIntArray)
        CheckMinMax(point)
    End Sub
    Public Sub Add_Ref(points As VectorOfPoint, höhen As Int32())
        If points.Size = höhen.Length Then
            _ReverenzenXY.Push(points)
            _ReverenzenZ.Push(höhen)
            For i = 0 To points.Size - 1
                Dim tmpMypoint As New MyPoint(points(i).X, points(i).Y, höhen(i))
                CheckMinMax(tmpMypoint)
            Next
        End If
    End Sub

    Public Sub Analyse()
        _MinAreaRec = CvInvoke.MinAreaRect(_ReverenzenXY)
    End Sub

    '--------------------------------------------------------------------------------------------------------------------------------------------------------
    'Funktionen
    '--------------------------------------------------------------------------------------------------------------------------------------------------------
    'Privat--------------------------------------------------
    Private Function Vergleich(v_x As Int32, v_y1 As Int32, v_y2 As Int32) As Boolean
        Return GetHöhe() = v_x And (GetBreite() = v_y1 Or GetBreite() = v_y2)
    End Function
    Private Function Vergleich(v_xmin As Double, v_xmax As Double, v_y1min As Double, v_y1max As Double, v_y2min As Double, v_y2max As Double) As Boolean
        Return (GetHöhe() >= v_xmin And GetHöhe() <= v_xmax) And ((GetBreite() >= v_y1min And GetBreite() <= v_y1max) Or (GetBreite() >= v_y2min And GetBreite() <= v_y2max))
    End Function
    Private Function VergleichF(HBF As Double, BTF As Double, HTF As Double) As Boolean
        Dim f As Double = GetFläche()
        Return f = HBF Or f = BTF Or f = HTF
    End Function
    Private Function VergleichF(HBF_xmin As Double, HBF_xmax As Double, BTF_min As Double, BTF_max As Double, HTF_min As Double, HTF_max As Double) As Boolean
        Dim f As Double = GetFläche()
        Return (f >= HBF_xmin And f <= HBF_xmax) Or (f >= BTF_min And f <= BTF_max) Or (f >= HTF_min And f <= HTF_max)
    End Function
    Private Function NormPoint(point As PointF) As Single
        Return CSng(Math.Sqrt(point.X ^ 2 + point.Y ^ 2))
    End Function

    'Public--------------------------------------------------
    Public Function HasRef(x As Int32, y As Int32) As Boolean
        For i = 0 To _ReverenzenXY.Size - 1
            If _ReverenzenXY(i).X = x And _ReverenzenXY(i).Y = y Then
                Return True
            End If
        Next
        Return False
    End Function


    Public Function Dist_X() As Int32
        Return _Max_X.X - _Min_X.X
    End Function
    Public Function Dist_Y() As Int32
        Return _Max_Y.Y - _Min_Y.Y
    End Function
    Public Function Dist_Z() As Int32
        Return _Max_Z.Z - _Min_Z.Z
    End Function
    Public Function Dist_Max() As Int32
        If Dist_X() >= Dist_Y() Then
            Return Dist_X()
        Else
            Return Dist_Y()
        End If
    End Function

    Public Function GetDimension(PixToMm As Double, zOffset As Int32) As Int32()
        Dim arr(3) As Int32
        arr(0) = CInt(Math.Round(GetHöhe(PixToMm)))
        arr(1) = CInt(Math.Round(GetBreite(PixToMm)))
        arr(2) = CInt(Math.Round(zOffset - GetDepthVal()))
        Array.Sort(arr)
        Return arr
    End Function

    Public Function Passend(höhe_mm As Int32, breite_mm As Int32, tiefe_mm As Int32, pixToMm As Double, zOffset As Int32, ByRef abweichung As Double, toleranz_prozent As Int32) As Boolean
        Dim uebereinstimmungPrc As Double = 0.0
        Dim soll As New List(Of Int32)
        Dim ist As New List(Of Int32)
        Dim dimensions() As Int32 = GetDimension(pixToMm, zOffset)
        ' Istwerte
        For i = 0 To 2
            ist.Add(dimensions(i))
        Next
        ' Sollwerte
        soll.Add(höhe_mm)
        soll.Add(breite_mm)
        soll.Add(tiefe_mm)
        soll.Sort()
        ' Vergleichen
        For i = 0 To 2
            uebereinstimmungPrc += If(ist(i) > soll(i), soll(i) / ist(i), ist(i) / soll(i)) / 3
        Next
        uebereinstimmungPrc = Math.Round(uebereinstimmungPrc * 100, 2)
        ' Ausgeben
        abweichung = uebereinstimmungPrc
        Return uebereinstimmungPrc > (100.0 - CDbl(toleranz_prozent))
    End Function

    Public Function PassendFläche(HBFläche_pix As Double, BTFläche_pix As Double, HTFläche_pix As Double, Optional toleranz_prozent As Int32 = 0) As Boolean
        Dim HBF_dif, BTF_dif, HTF_dif As Double
        If toleranz_prozent > 0 Then
            HBF_dif = (HBFläche_pix / 100) * toleranz_prozent
            BTF_dif = (BTFläche_pix / 100) * toleranz_prozent
            HTF_dif = (HTFläche_pix / 100) * toleranz_prozent
            Return VergleichF(HBFläche_pix - HBF_dif, HBFläche_pix + HBF_dif, BTFläche_pix - BTF_dif, BTFläche_pix + BTF_dif, HTFläche_pix - HTF_dif, HTFläche_pix + HTF_dif)
        Else
            Return VergleichF(HBFläche_pix, BTFläche_pix, HTFläche_pix)
        End If
    End Function

    Public Function PassendFläche2(HBFläche_pix As Double, BTFläche_pix As Double, HTFläche_pix As Double, Optional toleranz_prozent As Int32 = 0) As Boolean
        Dim HBF_dif, BTF_dif, HTF_dif As Double
        If toleranz_prozent > 0 Then
            HBF_dif = (HBFläche_pix / 100) * toleranz_prozent
            BTF_dif = (BTFläche_pix / 100) * toleranz_prozent
            HTF_dif = (HTFläche_pix / 100) * toleranz_prozent
            Return VergleichF(HBFläche_pix - HBF_dif, HBFläche_pix + HBF_dif, BTFläche_pix - BTF_dif, BTFläche_pix + BTF_dif, HTFläche_pix - HTF_dif, HTFläche_pix + HTF_dif)
        Else
            Return VergleichF(HBFläche_pix, BTFläche_pix, HTFläche_pix)
        End If
    End Function

    'Maase
    Public Function GetHöhe() As Double
        'Prüfen ob _MinAreaRec Angelegt
        If _MinAreaRec.Size.IsEmpty Then
            Analyse()
        End If
        If _MinAreaRec.Size.Height <= _MinAreaRec.Size.Width Then
            Return _MinAreaRec.Size.Height
        Else
            Return _MinAreaRec.Size.Width
        End If
    End Function
    Public Function GetHöhe(pixToMM As Double) As Double
        Return GetDepthVal() * GetHöhe() * pixToMM
    End Function
    Public Function GetBreite() As Double
        'Prüfen ob _MinAreaRec Angelegt
        If _MinAreaRec.Size.IsEmpty Then
            Analyse()
        End If
        If _MinAreaRec.Size.Height >= _MinAreaRec.Size.Width Then
            Return _MinAreaRec.Size.Height
        Else
            Return _MinAreaRec.Size.Width
        End If
    End Function
    Public Function GetBreite(pixToMM As Double) As Double
        Return GetDepthVal() * GetBreite() * pixToMM
    End Function
    Public Function GetFläche() As Int32
        Return CInt(Math.Round(GetBreite() * GetHöhe()))
    End Function
    Public Function GetFläche(pixToMM As Double) As Int32
        Return CInt(Math.Round(GetBreite(pixToMM) * GetHöhe(pixToMM)))
    End Function
    Public Function GetZentrumMyPoint() As MyPoint
        'Prüfen ob _MinAreaRec Angelegt
        If _MinAreaRec.Size.IsEmpty Then
            Analyse()
        End If
        Return New MyPoint(_MinAreaRec.Center, GetDepthVal())
    End Function
    Public Function GetZentrumPoint() As Point
        'Prüfen ob _MinAreaRec Angelegt
        If _MinAreaRec.Size.IsEmpty Then
            Analyse()
        End If
        Dim tmpPoint As New Point(CInt(Math.Round(_MinAreaRec.Center.X)), CInt(Math.Round(_MinAreaRec.Center.Y)))
        Return tmpPoint
    End Function
    Public Function GetZentrumPointF() As PointF
        'Prüfen ob _MinAreaRec Angelegt
        If _MinAreaRec.Size.IsEmpty Then
            Analyse()
        End If
        Return _MinAreaRec.Center
    End Function
    Public Function GetWinkel() As Double
        'Prüfen ob _MinAreaRec Angelegt
        If _MinAreaRec.Size.IsEmpty Then
            Analyse()
        End If
        'Winkel immer auf die Langeseite bezogen
        Dim Punkte(4) As PointF
        Punkte = _MinAreaRec.GetVertices

        Dim Kante1 As New PointF(Punkte(1).X - Punkte(0).X, Punkte(1).Y - Punkte(0).Y)
        Dim Kante2 As New PointF(Punkte(2).X - Punkte(1).X, Punkte(2).Y - Punkte(1).Y)
        Dim Norm1 As Single = NormPoint(Kante1)
        Dim Norm2 As Single = NormPoint(Kante2)
        Dim use As PointF = Kante1
        If Norm2 > Norm1 Then
            use = Kante2
        End If
        Dim Referenc As New PointF(1, 0) 'Horizontaler Vektor
        Norm1 = NormPoint(Referenc)
        Norm2 = NormPoint(use)
        Dim Vector As Double = Referenc.X * use.X + Referenc.Y + use.Y
        Dim Wert As Double = Vector / (Norm1 * Norm2)
        If Wert > 1 Then
            Wert = Wert - 1
        End If
        If Wert < -1 Then
            Wert = Wert + 1
        End If
        Dim WinkelRad = Math.Acos(Wert)
        Dim Winkel As Double = 180 / Math.PI * WinkelRad
        Return Winkel
    End Function
    Public Function GetWinkel2() As Double
        'Prüfen ob _MinAreaRec Angelegt
        If _MinAreaRec.Size.IsEmpty Then
            Analyse()
        End If
        'Winkel immer auf die Langeseite bezogen
        Dim Punkte(4) As PointF
        Punkte = _MinAreaRec.GetVertices
        'Winkel von Seite P0 zu P3 zur Horizontalen
        'Prüfen ob seite P0 zu P3 die lange seite (Breite ist)
        Dim Kante01 As New PointF(Punkte(1).X - Punkte(0).X, Punkte(1).Y - Punkte(0).Y)
        Dim Kante03 As New PointF(Punkte(3).X - Punkte(0).X, Punkte(3).Y - Punkte(0).Y)
        Dim LängeKante03 As Double = NormPoint(Kante03)
        Dim LängeKante01 As Double = NormPoint(Kante01)
        'Return If(LängeKante03 < GetBreite(), _MinAreaRec.Angle, _MinAreaRec.Angle + 90)
        Return If(LängeKante03 <= LängeKante01, _MinAreaRec.Angle, _MinAreaRec.Angle + 90)
    End Function

    Public Function GetDepthStr() As String
        Dim sum As Int64
        Dim min As Int32 = Int32.MaxValue
        Dim max As Int32 = Int32.MinValue
        For i = 0 To _ReverenzenZ.Size - 1
            If _ReverenzenZ(i) > max Then
                max = _ReverenzenZ(i)
            End If
            If _ReverenzenZ(i) < min Then
                min = _ReverenzenZ(i)
            End If
            sum += _ReverenzenZ(i)
        Next
        Dim midel As Int32 = CInt(Math.Round(sum / _ReverenzenZ.Size))
        Return $"Min:{min} Max:{max} Midel:{midel}"
    End Function
    Public Function GetDepthValAvg() As Double
        Dim sum As Int64
        Dim min As Int32 = Int32.MaxValue
        Dim max As Int32 = Int32.MinValue
        For i = 0 To _ReverenzenZ.Size - 1
            If _ReverenzenZ(i) > max Then
                max = _ReverenzenZ(i)
            End If
            If _ReverenzenZ(i) < min Then
                min = _ReverenzenZ(i)
            End If
            sum += _ReverenzenZ(i)
        Next
        Dim midel As Int32 = CInt(Math.Round(sum / _ReverenzenZ.Size))
        Return midel
    End Function

    Public Function GetDepthVal() As Int32
        Dim center As Point = GetZentrumPoint()
        Dim tmpZ As Int32 = 0
        Dim cnt As Integer = 0

        For i = 0 To _ReverenzenXY.Size - 1
            If _ReverenzenXY(i).X <= center.X + 2 And _ReverenzenXY(i).X >= center.X - 2 And
                _ReverenzenXY(i).Y <= center.Y + 2 And _ReverenzenXY(i).Y >= center.Y - 2 Then
                cnt += 1
                tmpZ += _ReverenzenZ(i)
            End If
        Next

        Return CInt(Math.Round(tmpZ / cnt, 0))
    End Function


    Public Function GetContour() As VectorOfPoint
        Return _ReverenzenXY
    End Function
    Public Function GetContours() As VectorOfVectorOfPoint
        Dim tmpV As New VectorOfVectorOfPoint
        tmpV.Push(_ReverenzenXY)
        Return tmpV
    End Function

    Public Function GetOuterPoints() As Point()
        Dim tmpPoints(3) As Point
        tmpPoints(0) = New Point(_Min_X.X, _Min_Y.Y)
        tmpPoints(1) = New Point(_Max_X.X, _Min_Y.Y)
        tmpPoints(2) = New Point(_Max_X.X, _Max_Y.Y)
        tmpPoints(3) = New Point(_Min_X.X, _Max_Y.Y)
        Return tmpPoints
    End Function
    Public Function GetMinAreaPoints() As Point()
        Dim tmpPoints(3) As Point
        Dim tmpPointf() As PointF
        tmpPointf = _MinAreaRec.GetVertices
        For i = 0 To 3
            tmpPoints(i).X = CInt(Math.Round(tmpPointf(i).X))
            tmpPoints(i).Y = CInt(Math.Round(tmpPointf(i).Y))
        Next
        Return tmpPoints
    End Function
    Public Function GetMinAreaPointFs() As PointF()
        Return _MinAreaRec.GetVertices
    End Function

    '--------------------------------------------------------------------------------------------------------------------------------------------------------
    'Overrides
    '--------------------------------------------------------------------------------------------------------------------------------------------------------
    Public Overrides Function ToString() As String
        Return ($"{_ID,3}:Zentrum:{GetZentrumPoint.ToString()} ; Höhe:{GetHöhe(),4:n2} ; Breite: {GetBreite(),4:n2} ; Fläche:{GetFläche(),4} ; Winkel: {GetWinkel2.ToString("N2"),6} | {_MinAreaRec.Angle,6:n2}")
    End Function

End Class
