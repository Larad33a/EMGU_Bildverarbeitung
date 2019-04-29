Imports System.Xml
Imports System.Xml.Schema
Imports System.Xml.Serialization

Public Class SearchObj : Implements IXmlSerializable

    Private _Name As String
    Public Property Name() As String
        Get
            Return _Name
        End Get
        Set(ByVal value As String)
            _Name = value
        End Set
    End Property
    Private _Höhe As Int32
    Public Property Höhe() As Int32
        Get
            Return _Höhe
        End Get
        Set(ByVal value As Int32)
            _Höhe = value
        End Set
    End Property
    Private _Breite As Int32
    Public Property Beite() As Int32
        Get
            Return _Breite
        End Get
        Set(ByVal value As Int32)
            _Breite = value
        End Set
    End Property
    Private _Tiefe As Int32
    Public Property Tiefe() As Int32
        Get
            Return _Tiefe
        End Get
        Set(ByVal value As Int32)
            _Tiefe = value
        End Set
    End Property
    Private _ID As Int32
    Public Property ID() As Int32
        Get
            Return _ID
        End Get
        Set(ByVal value As Int32)
            _ID = value
        End Set
    End Property


    Sub New()

    End Sub

    Sub New(id As Int32, name As String, h As Int32, b As Int32, t As Int32)
        _ID = id
        _Name = name
        _Höhe = h
        _Breite = b
        _Tiefe = t
    End Sub

    Public Function Maase() As String
        Return $"H|B|T: {_Höhe,4} | {_Breite,4} | {_Tiefe,4}"
    End Function

    Public Overrides Function ToString() As String
        Return $"{_ID,3}: {_Name} [{_Höhe,4} | {_Breite,4} | {_Tiefe,4}]"
    End Function

    Public Function GetSchema() As XmlSchema Implements IXmlSerializable.GetSchema
        Throw New NotImplementedException()
    End Function

    Public Sub ReadXml(reader As XmlReader) Implements IXmlSerializable.ReadXml
        Throw New NotImplementedException()
    End Sub

    Public Sub WriteXml(writer As XmlWriter) Implements IXmlSerializable.WriteXml
        Throw New NotImplementedException()
    End Sub
End Class
