import React, {
  useEffect,
  useState,
  createContext,
  useCallback,
  useContext,
  SetStateAction
} from 'react';
import {
  NativeEventEmitter,
  Alert,
  Platform,
  AppState,
  AppStateStatus
} from 'react-native';

import ExposureNotification, {
  AuthorisedStatus,
  StatusState,
  Status,
  CloseContact,
  StatusType,
  KeyServerType
} from './exposure-notification-module';

import {getPermissions, requestPermissions} from './utils/permissions';
import {
  ExposurePermissions,
  PermissionStatus,
  TraceConfiguration
} from './types';

const emitter = new NativeEventEmitter(ExposureNotification);

interface State {
  status: Status;
  supported: boolean;
  canSupport: boolean;
  isAuthorised: AuthorisedStatus;
  enabled: boolean;
  contacts?: CloseContact[];
  initialised: boolean;
  permissions: ExposurePermissions;
}

export interface ExposureContextValue extends State {
  start: () => void;
  stop: () => void;
  configure: () => void;
  checkExposure: (readDetails: boolean, skipTimeCheck: boolean) => void;
  simulateExposure: (timeDelay: number) => void;
  getDiagnosisKeys: () => Promise<any[]>;
  exposureEnabled: () => Promise<boolean>;
  authoriseExposure: () => Promise<boolean>;
  deleteAllData: () => Promise<void>;
  supportsExposureApi: () => Promise<void>;
  getCloseContacts: () => Promise<CloseContact[] | null>;
  getLogData: () => Promise<{[key: string]: any}>;
  triggerUpdate: () => Promise<string | undefined>;
  deleteExposureData: () => Promise<void>;
  readPermissions: () => Promise<void>;
  askPermissions: () => Promise<void>;
  setExposureState: (setStateAction: SetStateAction<State>) => void;
}

const initialState = {
  status: {
    state: StatusState.unavailable,
    type: [StatusType.starting]
  },
  supported: false,
  canSupport: false,
  isAuthorised: 'unknown' as AuthorisedStatus,
  enabled: false,
  contacts: [] as CloseContact[],
  initialised: false,
  permissions: {
    exposure: {status: PermissionStatus.Unknown},
    notifications: {status: PermissionStatus.Unknown}
  }
};

export const ExposureContext = createContext<ExposureContextValue>({
  ...initialState,
  start: () => {},
  stop: () => {},
  configure: () => {},
  checkExposure: () => {},
  simulateExposure: () => {},
  getDiagnosisKeys: () => Promise.resolve([]),
  exposureEnabled: () => Promise.resolve(false),
  authoriseExposure: () => Promise.resolve(false),
  deleteAllData: () => Promise.resolve(),
  supportsExposureApi: () => Promise.resolve(),
  getCloseContacts: () => Promise.resolve([]),
  getLogData: () => Promise.resolve({}),
  triggerUpdate: () => Promise.resolve(undefined),
  deleteExposureData: () => Promise.resolve(),
  readPermissions: () => Promise.resolve(),
  askPermissions: () => Promise.resolve(),
  setExposureState: () => {}
});

export interface ExposureProviderProps {
  isReady: boolean;
  traceConfiguration: TraceConfiguration;
  appVersion: string;
  serverUrl: string;
  keyServerUrl: string;
  keyServerType: KeyServerType;
  authToken: string;
  refreshToken: string;
  notificationTitle: string;
  notificationDescription: string;
  callbackNumber?: string;
  analyticsOptin?: boolean;
}

export const ExposureProvider: React.FC<ExposureProviderProps> = ({
  children,
  isReady = false,
  traceConfiguration,
  appVersion,
  serverUrl,
  keyServerUrl,
  keyServerType = KeyServerType.nearform,
  authToken = "Qk32EZw55f2I00ECpfoem6xiNuUXL5yNUneUHvNy5qFxPWr0kKEbY4GQG1mwAoTFjd9tBm9kdirvhNI",
  refreshToken = '',
  notificationTitle,
  notificationDescription,
  callbackNumber = '',
  analyticsOptin = false
}) => {
  const [state, setState] = useState<State>(initialState);

  useEffect(() => {
    function handleEvent(
      ev: {onStatusChanged?: Status; status?: any; scheduledTask?: any} = {}
    ) {
      console.log(`exposureEvent: ${JSON.stringify(ev)}`);
      if (ev.onStatusChanged) {
        return validateStatus(ev.onStatusChanged);
      }
    }

    let subscription = emitter.addListener('exposureEvent', handleEvent);

    const listener = (type: AppStateStatus) => {
      if (type === 'active') {
        validateStatus();
        getCloseContacts();
      }
    };

    AppState.addEventListener('change', listener);

    return () => {
      subscription.remove();
      emitter.removeListener('exposureEvent', handleEvent);
      AppState.removeEventListener('change', listener);
    };
  }, []);

  useEffect(() => {
    async function checkSupportAndStart() {
      await supportsExposureApi();

      // Start as soon as we're able to
      if (
        isReady &&
        state.permissions.exposure.status === PermissionStatus.Allowed
      ) {
        await configure();
        start();
      }
    }

    checkSupportAndStart();
  }, [state.permissions, isReady]);

  const supportsExposureApi = async function () {
    const can = await ExposureNotification.canSupport();
    const is = await ExposureNotification.isSupported();
    const status = await ExposureNotification.status();
    const enabled = await ExposureNotification.exposureEnabled();
    const isAuthorised = await ExposureNotification.isAuthorised();

    setState((s) => ({
      ...s,
      status,
      enabled,
      canSupport: can,
      supported: is,
      isAuthorised
    }));
    await validateStatus(status);
    if (enabled) {
      getCloseContacts();
    }
  };

  const validateStatus = async (status?: Status) => {
    let newStatus = status || ((await ExposureNotification.status()) as Status);
    const enabled = await ExposureNotification.exposureEnabled();
    const isAuthorised = await ExposureNotification.isAuthorised();
    const canSupport = await ExposureNotification.canSupport();

    const isStarting =
      (isAuthorised === AuthorisedStatus.unknown ||
        isAuthorised === AuthorisedStatus.granted) &&
      newStatus.state === StatusState.unavailable &&
      newStatus.type?.includes(StatusType.starting);
    const initialised = !isStarting || !canSupport;
    setState((s) => ({
      ...s,
      status: newStatus,
      enabled,
      isAuthorised,
      canSupport,
      initialised
    }));
  };

  const start = async () => {
    try {
      await ExposureNotification.start();
      await validateStatus();
      await getCloseContacts();
    } catch (err) {
      console.log('start err', err);
    }
  };

  const stop = async () => {
    try {
      await ExposureNotification.stop();
      await validateStatus();
    } catch (err) {
      console.log('stop err', err);
    }
  };

  const configure = async () => {
    try {
      const iosLimit =
        traceConfiguration.fileLimitiOS > 0
          ? traceConfiguration.fileLimitiOS
          : traceConfiguration.fileLimit;

      const config = {
        exposureCheckFrequency: traceConfiguration.exposureCheckInterval,
        serverURL: serverUrl,
        keyServerUrl,
        keyServerType,
        authToken,
        refreshToken,
        storeExposuresFor: traceConfiguration.storeExposuresFor,
        fileLimit:
          Platform.OS === 'ios' ? iosLimit : traceConfiguration.fileLimit,
        version: appVersion,
        notificationTitle,
        notificationDesc: notificationDescription,
        callbackNumber,
        analyticsOptin
      };

      await ExposureNotification.configure(config);

      return true;
    } catch (err) {
      console.log('configure err', err);
      return false;
    }
  };

  const checkExposure = (readDetails: boolean, skipTimeCheck: boolean) => {
    ExposureNotification.checkExposure(readDetails, skipTimeCheck);
  };

  const simulateExposure = (timeDelay: number) => {
    ExposureNotification.simulateExposure(timeDelay);
  };

  const getDiagnosisKeys = () => {
    return ExposureNotification.getDiagnosisKeys();
  };

  const exposureEnabled = async () => {
    return ExposureNotification.exposureEnabled();
  };

  const authoriseExposure = async () => {
    return ExposureNotification.authoriseExposure();
  };

  const deleteAllData = async () => {
    await ExposureNotification.deleteAllData();
    await validateStatus();
  };

  const getCloseContacts = async () => {
    try {
      if (state.permissions.exposure.status === PermissionStatus.Allowed) {
        const contacts = await ExposureNotification.getCloseContacts();
        setState((s) => ({...s, contacts}));
        return contacts;
      }
      return [];
    } catch (err) {
      console.log('getCloseContacts err', err);
      return null;
    }
  };

  const getLogData = async () => {
    try {
      const data = await ExposureNotification.getLogData();
      return data;
    } catch (err) {
      console.log('getLogData err', err);
      return null;
    }
  };

  const triggerUpdate = async () => {
    try {
      const result = await ExposureNotification.triggerUpdate();
      console.log('trigger update: ', result);
      // this will not occur after play services update available to public
      if (result === 'api_not_available') {
        Alert.alert(
          'API Not Available',
          'Google Exposure Notifications API not available on this device yet'
        );
      }
      return result;
    } catch (e) {
      console.log('trigger update error', e);
    }
  };

  const deleteExposureData = async () => {
    try {
      await ExposureNotification.deleteExposureData();
      setState((s) => ({...s, contacts: []}));
    } catch (e) {
      console.log('delete exposure data error', e);
    }
  };

  const readPermissions = useCallback(async () => {
    console.log('Read permissions...');

    const perms = await getPermissions();
    console.log('perms: ', JSON.stringify(perms, null, 2));

    setState((s) => ({...s, permissions: perms}));
  }, []);

  const askPermissions = useCallback(async () => {
    console.log('Requesting permissions...', state.permissions);
    await requestPermissions();

    await readPermissions();
  }, []);

  useEffect(() => {
    readPermissions();
  }, [readPermissions]);

  const value: ExposureContextValue = {
    ...state,
    start,
    stop,
    configure,
    checkExposure,
    simulateExposure,
    getDiagnosisKeys,
    exposureEnabled,
    authoriseExposure,
    deleteAllData,
    supportsExposureApi,
    getCloseContacts,
    getLogData,
    triggerUpdate,
    deleteExposureData,
    readPermissions,
    askPermissions,
    setExposureState: setState
  };

  return (
    <ExposureContext.Provider value={value}>
      {children}
    </ExposureContext.Provider>
  );
};

export const useExposure = () => useContext(ExposureContext);
