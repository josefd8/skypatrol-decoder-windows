Imports System.Threading
Imports System.Net
Imports System.ComponentModel.Component

''' <summary>
''' Clase para manejo de funciones generales
''' </summary>
''' <remarks></remarks>
Public Class ClassGeneral

    ''' <summary>
    ''' Estructura que encapsula las propiedades de un paquete entrante
    ''' </summary>
    ''' <remarks></remarks>
    <Serializable()> Public Structure Paquete_Entrante
        Public Datos_Str As String
        Public Datos_Byte() As Byte
        Public Datos_Hex As String
        Public ID_Equipo As String
        Public Ip As String
        Public Puerto As Long
        Public Id_Socket As Long
        Public Tipo_Consola As Integer
        Public cliente As System.Net.IPEndPoint
    End Structure

    Public Structure Propiedades_SQL_UDP
        Public SQLUDP As Boolean
        Public SQL_UDP_Coleccion As Collections.Queue
        Public IndexUDPMax As Integer
        Public IndexUDP As Integer
    End Structure

End Class

