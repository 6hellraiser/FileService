# FileService
При запуске сервиса на диске C создаётся директория Service, при создании текстовых файлов в которой результат отправляется на другие
машины.
В файле конфигурации задаются следующие параметры:

  <listenAddressGroup>
    <listenAddress>
      <add key="1.2.3.4"/>
    </listenAddress>
  </listenAddressGroup>
  В этой части указывается ip машины, от которой сервис ожидает прибытия файлов.
  
  <client>
      <endpoint address="net.p2p://1.2.3.4/FileService" binding="netPeerTcpBinding"
              bindingConfiguration="Wimpy" contract="Backend.IBackend" name="2"
              kind="" endpointConfiguration="" />
  </client>
  В этой части перечислены все узлы, которым данный сервис отсылает данные.
  
  <service name="Backend.Backend">
        <endpoint address="net.p2p://1.2.3.4/FileService" binding="netPeerTcpBinding"
            bindingConfiguration="Wimpy" name="Chat" contract="Backend.IBackend" />
  </service>
  В ээтой части указывается адрес машины, на которой установлен сервис.
