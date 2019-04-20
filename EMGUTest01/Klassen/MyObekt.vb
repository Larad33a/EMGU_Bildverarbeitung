Public Class MyObekt
    Private _ID As Int32
    Public Property ID() As Int32
        Get
            Return _ID
        End Get
        Set(ByVal value As Int32)
            _ID = value
        End Set
    End Property
    Private _Max_X As MyPoint
    Public ReadOnly Property Max_X() As MyPoint
        Get
            Return _Max_X
        End Get
    End Property
    Private _Min_X As MyPoint
    Public ReadOnly Property Min_X() As MyPoint
        Get
            Return _Min_X
        End Get
    End Property
    Private _Max_Y As MyPoint
    Public ReadOnly Property Max_Y() As MyPoint
        Get
            Return _Max_Y
        End Get
    End Property
    Private _Min_Y As MyPoint
    Public ReadOnly Property Min_Y() As MyPoint
        Get
            Return _Min_Y
        End Get
    End Property
    Private _Max_Z As MyPoint
    Public ReadOnly Property Max_Z() As MyPoint
        Get
            Return _Max_Z
        End Get
    End Property
    Private _Min_Z As MyPoint
    Public ReadOnly Property Min_Z() As MyPoint
        Get
            Return _Min_Z
        End Get
    End Property
    Private _Color As Byte()
    Public Property Color() As Byte()
        Get
            Return _Color
        End Get
        Set(ByVal value As Byte())
            _Color = value
        End Set
    End Property

    Private _Reverenzen As New List(Of MyPoint)

    Sub New()

    End Sub

    Sub New(id As Int32)
        _ID = id
    End Sub
    Sub New(id As Int32, color As Byte())
        _ID = id
        _Color = color
    End Sub

    Public Sub Add_Ref(x As Int32, y As Int32, z As Int32)
        Dim tmpPoint As New MyPoint(x, y, z)
        _Reverenzen.Add(tmpPoint)
        CheckMinMax(tmpPoint)
    End Sub
    Public Sub Add_Ref(point As MyPoint)
        _Reverenzen.Add(point)
        CheckMinMax(point)
    End Sub
    Private Sub CheckMinMax(point As MyPoint)
        If _Reverenzen.Count <= 1 Then 'Erster Punkt
            _Max_X = point
            _Min_X = point
            _Max_Y = point
            _Min_Y = point
            _Max_Z = point
            _Min_Z = point
        Else 'Es sind bereits Punkte vorhanden
            If point.X > _Max_X.X Then
                _Max_X = point
            End If
            If point.X < _Min_X.X Then
                _Min_X = point
            End If
            If point.Y > _Max_Y.Y Then
                _Max_Y = point
            End If
            If point.Y < _Min_Y.Y Then
                _Min_Y = point
            End If
            If point.Z > _Max_Z.Z Then
                _Max_Z = point
            End If
            If point.Z < _Min_Z.Z Then
                _Min_Z = point
            End If
        End If
    End Sub
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

    Public Function Passend(länge_mm As Int32, breit_mm As Int32, tiefe_mm As Int32, Optional toleranz_prozent As Int32 = 0) As Boolean
        Dim länge_dif, breite_dif, tiefe_dif As Double
        If toleranz_prozent > 0 Then
            länge_dif = (länge_mm / 100) * toleranz_prozent
            breite_dif = (breit_mm / 100) * toleranz_prozent
            tiefe_dif = (tiefe_mm / 100) * toleranz_prozent
            Return (Vergleich(länge_mm - länge_dif, länge_mm + länge_dif, breit_mm - breite_dif, breit_mm + breite_dif, tiefe_mm - tiefe_dif, tiefe_mm + tiefe_dif) Or Vergleich(breit_mm - breite_dif, breit_mm + breite_dif, länge_mm - länge_dif, länge_mm + länge_dif, tiefe_mm - tiefe_dif, tiefe_mm + tiefe_dif) Or Vergleich(tiefe_mm - tiefe_dif, tiefe_mm + tiefe_dif, länge_mm - länge_dif, länge_mm + länge_dif, breit_mm - breite_dif, breit_mm + breite_dif))
        Else
            Return (Vergleich(länge_mm, breit_mm, tiefe_mm) Or Vergleich(breit_mm, länge_mm, tiefe_mm) Or Vergleich(tiefe_mm, länge_mm, breit_mm))
        End If
    End Function

    Private Function Vergleich(v_x As Int32, v_y1 As Int32, v_y2 As Int32) As Boolean
        Return Dist_X() = v_x And (Dist_Y() = v_y1 Or Dist_Y() = v_y2)
    End Function
    Private Function Vergleich(v_xmin As Double, v_xmax As Double, v_y1min As Double, v_y1max As Double, v_y2min As Double, v_y2max As Double) As Boolean
        Return (Dist_X() >= v_xmin And Dist_X() <= v_xmax) And ((Dist_Y() >= v_y1min And Dist_Y() <= v_y1max) Or (Dist_Y() >= v_y2min And Dist_Y() <= v_y2max))
    End Function

    Public Overrides Function ToString() As String
        Return ($"{_ID,3}: Xmin:{_Min_X.ToString()} ; Xdist:{Dist_X(),4} ; Ymin:{_Min_Y.ToString()} ; Ydist:{Dist_Y(),4} ; Z:{_Min_Z.ToString} - {_Max_Z.ToString}")
    End Function
End Class
