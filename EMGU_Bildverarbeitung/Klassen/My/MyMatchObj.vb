
Public Class MyMatchObj : Implements IComparable(Of MyMatchObj)

    Private _Uebereinstimmung As Double
    Public Property Uebereinstimmung() As Double
        Get
            Return _Uebereinstimmung
        End Get
        Set(ByVal value As Double)
            _Uebereinstimmung = value
        End Set
    End Property
    Private _Objekt As MyObjektV2
    Public Property Objekt() As MyObjektV2
        Get
            Return _Objekt
        End Get
        Set(ByVal Objekt As MyObjektV2)
            _Objekt = Objekt
        End Set
    End Property
    Private _Ausrichtung As String
    Public Property Ausrichtung() As String
        Get
            Return _Ausrichtung
        End Get
        Set(ByVal value As String)
            _Ausrichtung = value
        End Set
    End Property
    Sub New(übereinstimmung As Double, ausrichtung As String, objekt As MyObjektV2)
        _Uebereinstimmung = übereinstimmung
        _Ausrichtung = ausrichtung
        _Objekt = objekt
    End Sub

    Function Compareto(obj As MyMatchObj) As Integer _
        Implements IComparable(Of MyMatchObj).CompareTo
        Return _Uebereinstimmung.CompareTo(obj.Uebereinstimmung)
    End Function
    Public Overrides Function ToString() As String
        Return $"{_Uebereinstimmung,3:n2}% | {_Ausrichtung} | ID: {_Objekt.ID}"
    End Function
End Class
