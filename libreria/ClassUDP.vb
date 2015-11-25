#Region "Información"
#End Region

Imports System
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

''' <summary>
''' Clase para el manejo de conexionees UDP (cliente y servidor)
''' </summary>
''' <remarks></remarks>
Public Class ClassUDP

#Region "Variables"
    Private UDP_Client As New UdpClient
    Public thdUdp As Thread
    Public UDP_Server As UdpClient
    Private m_PuertoLocal As String
    Private m_PuertoRemoto As String
    Private m_IpLocal As IPAddress
    Private m_IpRemoto As IPAddress
    Public Estado As Boolean
    Public Mensaje As String
    Public m_Id_Unico As Integer
    Private DataIn As New libreria.ClassGeneral.Paquete_Entrante

#End Region

#Region "Eventos"
    Public Event Close()
    ''' <summary>
    ''' Este metodo se dispara de forma asincrona cada vez que el socket reibe un nuevo mensaje
    ''' </summary>
    ''' <param name="Data">Detalles del mensaje y del cliente como un objeto tipo Paquete_Entrante</param>
    ''' <remarks></remarks>
    Public Event DataArrival(ByVal Data As libreria.ClassGeneral.Paquete_Entrante)
    ''' <summary>
    ''' Se dispara uando se presenta un error en el socket
    ''' </summary>
    ''' <param name="Description">Mensaj de error identficador del socket que produce el error</param>
    ''' <param name="Id"></param>
    ''' <remarks></remarks>
    Public Event SockError(ByVal Description As String, ByVal Id As Integer)
#End Region

#Region "Propiedades"

    'Asignar Puerto Local
    Property PuertoLocal() As String
        Get
            PuertoLocal = m_PuertoLocal
        End Get
        Set(ByVal Value As String)
            m_PuertoLocal = Value
        End Set
    End Property

    'Asignar Ip Local
    Property IpLocal() As IPAddress
        Get
            IpLocal = m_IpLocal
        End Get
        Set(ByVal Value As IPAddress)
            m_IpLocal = Value
        End Set
    End Property

    'Asignar Puerto Remoto
    Property PuertoRemoto() As String
        Get
            PuertoRemoto = m_PuertoRemoto
        End Get
        Set(ByVal Value As String)
            m_PuertoRemoto = Value
        End Set
    End Property

    'Asignar Ip Remoto
    Property IpRemoto() As IPAddress
        Get
            IpRemoto = m_IpRemoto
        End Get
        Set(ByVal Value As IPAddress)
            m_IpRemoto = Value
        End Set
    End Property

    'Asignar Id Unico
    Property Id_Unico() As Integer
        Get
            Id_Unico = m_Id_Unico
        End Get
        Set(ByVal Value As Integer)
            m_Id_Unico = Value
        End Set
    End Property

#End Region

#Region "Métodos"
    Public Sub UDP_Send(ByVal Data As String)
        Try
            UDP_Client.Connect(IpRemoto, PuertoRemoto)
            Dim sendBytes As [Byte]() = Encoding.Default.GetBytes(Data)
            UDP_Client.Send(sendBytes, sendBytes.Length)
        Catch e As Exception
            RaiseEvent SockError(e.Message, m_Id_Unico)
            Me.Estado = False
            Me.Mensaje = e.Message
        End Try
    End Sub

    Public Sub UDP_Send(ByVal Data() As Byte)
        Try
            UDP_Client.Connect(IpRemoto, PuertoRemoto)
            Dim sendBytes As [Byte]() = Data
            UDP_Client.Send(sendBytes, sendBytes.Length)

        Catch e As Exception
            RaiseEvent SockError(e.Message, m_Id_Unico)
            Me.Estado = False
            Me.Mensaje = e.Message
        End Try
    End Sub

    Public Sub Enviar(ByVal DatosBytes() As Byte, ByVal Ip As String, ByVal Puerto As Integer)
        Try
            Dim ElSocket As New Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            Dim Destino As New IPEndPoint(IPAddress.Parse(Ip), Puerto)
            ElSocket.SendTo(DatosBytes, DatosBytes.Length, SocketFlags.None, Destino)
        Catch ex As Exception
            RaiseEvent SockError(ex.Message, m_Id_Unico)
        End Try
    End Sub

    Public Sub UDP_Send_Server(ByVal Data() As Byte)
        Try
            UDP_Server.Connect(IpRemoto, PuertoRemoto)
            Dim sendBytes As [Byte]() = Data
            UDP_Server.Send(sendBytes, sendBytes.Length)
        Catch e As Exception
            RaiseEvent SockError(e.Message, m_Id_Unico)
            Me.Estado = False
            Me.Mensaje = e.Message
        End Try
    End Sub

    Public Sub UDP_Listen()
        Try
            Dim ConexionLocal As IPEndPoint
            ConexionLocal = New IPEndPoint(IpLocal, PuertoLocal)
            UDP_Server = New UdpClient(ConexionLocal)
            thdUdp = New Thread(AddressOf GetUDPData)
            thdUdp.Start()
            Me.Estado = True
        Catch e As Exception
            RaiseEvent SockError(e.Message, m_Id_Unico)
            'RaiseEvent DataArrival(e.ToString)
            Me.Estado = False
            Me.Mensaje = e.Message
        End Try
    End Sub

    Public Sub CloseSock()
        Try
            Thread.Sleep(30)
            If Not Me.UDP_Server Is Nothing Then
                Me.UDP_Server.Close()
                Me.thdUdp.Abort()
            End If
            Me.Estado = False
        Catch ex As Exception
            Me.Estado = False
            Me.Mensaje = ex.Message
        End Try

    End Sub

    Public Function Obtener_Ip(Optional ByVal Argumento As String = "") As String
        Obtener_Ip = ""
        Dim i_cont As Integer
        Dim Host As String
        ' Si no se pasa como parametro un nombre, muestra las ip locales
        If Environment.GetCommandLineArgs().Length > 1 Then
            Host = Environment.GetCommandLineArgs(1)
        Else
            Host = Dns.GetHostName
        End If

        Dim IPs As IPHostEntry = Dns.GetHostEntry(Host)
        Dim Direcciones As IPAddress() = IPs.AddressList

        'Se despliega la lista de IP's
        For i_cont = 0 To i_cont = Direcciones.Length
            Console.WriteLine("IP {0}: {1} ", i_cont + 1, Direcciones(i_cont).ToString())
            Obtener_Ip = Direcciones(i_cont).ToString()
        Next
        Return Obtener_Ip
    End Function

    Public Function Obtener_IpS(Optional ByVal Argumento As String = "") As Collection
        Dim salida As New Collection
        Dim Host As String

        ' Si no se pasa como parametro un nombre, muestra las ip locales
        If Environment.GetCommandLineArgs().Length > 1 Then
            Host = Environment.GetCommandLineArgs(1)
        Else
            Host = Dns.GetHostName
        End If

        Dim IPs As IPHostEntry = Dns.GetHostEntry(Host)

        For Each direccion As IPAddress In IPs.AddressList
            salida.Add(direccion.ToString)
        Next

        Return salida
    End Function

#End Region

#Region "Funciones Privadas"
    Private Sub GetUDPData()
        Do While True
            Try
                Dim RemoteIpEndPoint As New IPEndPoint(IPAddress.Any, 0)
                Dim RData As Object = UDP_Server.Receive(RemoteIpEndPoint)

                DataIn.Datos_Byte = RData
                DataIn.Ip = RemoteIpEndPoint.Address.ToString
                DataIn.Puerto = RemoteIpEndPoint.Port
                DataIn.Id_Socket = m_Id_Unico
               
                RaiseEvent DataArrival(DataIn)
                Thread.Sleep(0)
            Catch e As Exception
                'RaiseEvent SockError(e.Message, m_Id_Unico)
            End Try
        Loop
    End Sub
#End Region

End Class

