
Public Class MyMatchObj
    Implements IComparable(Of MyMatchObj)
    Private _Prozent As Double
    Public Property Prozent() As Double
        Get
            Return _Prozent
        End Get
        Set(ByVal Prozent As Double)
            _Prozent = Prozent
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
    Sub New(prozent As Double, ausrichtung As String, objekt As MyObjektV2)
        _Prozent = prozent
        _Ausrichtung = ausrichtung
        _Objekt = objekt
    End Sub

    Function Compareto(obj As MyMatchObj) As Integer _
        Implements IComparable(Of MyMatchObj).CompareTo
        Return _Prozent.CompareTo(obj.Prozent)
    End Function
    Public Overrides Function ToString() As String
        Return $"{_Prozent,3} | {_Ausrichtung} | ID: {_Objekt.ID}"
    End Function
End Class
