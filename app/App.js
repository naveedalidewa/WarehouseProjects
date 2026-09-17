import { useEffect, useState } from 'react';
import {
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  SafeAreaView,
  StatusBar,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from 'react-native';
import * as SecureStore from 'expo-secure-store';

const API_URL = process.env.EXPO_PUBLIC_API_URL;
const SESSION_KEY = 'customer_session';

export default function App() {
  // 'loading' while we check for a stored session, then 'login' | 'pending' | 'signedin'
  const [view, setView] = useState('loading');
  const [session, setSession] = useState(null);
  const [email, setEmail] = useState('mario@mariocafe.com');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [pendingMessage, setPendingMessage] = useState('');
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    SecureStore.getItemAsync(SESSION_KEY).then((raw) => {
      if (raw) {
        setSession(JSON.parse(raw));
        setView('signedin');
      } else {
        setView('login');
      }
    });
  }, []);

  async function handleLogin() {
    setError('');
    setSubmitting(true);
    try {
      const res = await fetch(`${API_URL}/api/customer/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      });
      const data = await res.json();

      if (res.status === 403) {
        // Not-approved customer: API already tailors the message to
        // Pending / OnHold / Rejected, so just show it as-is.
        setPendingMessage(data.message);
        setView('pending');
        return;
      }
      if (!res.ok) {
        setError(data.message || 'Sign in failed.');
        return;
      }

      await SecureStore.setItemAsync(SESSION_KEY, JSON.stringify(data));
      setSession(data);
      setView('signedin');
    } catch {
      setError('Could not reach the API. Is it running?');
    } finally {
      setSubmitting(false);
    }
  }

  async function handleLogOut() {
    await SecureStore.deleteItemAsync(SESSION_KEY);
    setSession(null);
    setPassword('');
    setView('login');
  }

  if (view === 'loading') {
    return (
      <SafeAreaView style={[styles.safe, styles.centeredScreen]}>
        <ActivityIndicator color="#124559" />
      </SafeAreaView>
    );
  }

  if (view === 'signedin') {
    return (
      <SafeAreaView style={styles.safe}>
        <StatusBar barStyle="dark-content" />
        <View style={styles.centeredScreen}>
          <View style={[styles.ring, styles.ringOk]}>
            <Text style={[styles.ringGlyph, { color: GREEN_ICON }]}>✓</Text>
          </View>
          <Text style={styles.h2}>Signed in as {session.displayName}</Text>
          <Text style={styles.p}>{session.email}</Text>
          <TouchableOpacity style={[styles.btn, styles.btnGhost, styles.mt24]} onPress={handleLogOut}>
            <Text style={styles.btnGhostText}>Log out</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  if (view === 'pending') {
    return (
      <SafeAreaView style={styles.safe}>
        <StatusBar barStyle="dark-content" />
        <View style={styles.centeredScreen}>
          <View style={[styles.ring, styles.ringWarn]}>
            <Text style={[styles.ringGlyph, { color: AMBER_ICON }]}>!</Text>
          </View>
          <Text style={styles.h2}>Account under review</Text>
          <Text style={styles.p}>{pendingMessage}</Text>
          <TouchableOpacity
            style={[styles.btn, styles.btnGhost, styles.mt24]}
            onPress={() => setView('login')}
          >
            <Text style={styles.btnGhostText}>Back to log in</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.safe}>
      <StatusBar barStyle="dark-content" />
      <KeyboardAvoidingView
        style={styles.flex1}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <View style={styles.login}>
          <View style={styles.logo}>
            <View style={styles.logoMark}>
              <Text style={styles.logoGlyph}>S</Text>
            </View>
            <Text style={styles.wordmark}>Stockroom</Text>
            <Text style={styles.tagline}>Wholesale trade portal</Text>
          </View>

          <View style={styles.field}>
            <Text style={styles.label}>Email</Text>
            <TextInput
              style={styles.input}
              value={email}
              onChangeText={setEmail}
              autoCapitalize="none"
              autoCorrect={false}
              keyboardType="email-address"
            />
          </View>
          <View style={styles.field}>
            <Text style={styles.label}>Password</Text>
            <TextInput
              style={styles.input}
              value={password}
              onChangeText={setPassword}
              secureTextEntry
            />
          </View>

          {error ? <Text style={styles.error}>{error}</Text> : null}

          <TouchableOpacity
            style={[styles.btn, styles.btnPrimary]}
            onPress={handleLogin}
            disabled={submitting}
          >
            <Text style={styles.btnPrimaryText}>{submitting ? 'Logging in…' : 'Log in'}</Text>
          </TouchableOpacity>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const TEAL = '#124559';
const AMBER_RING = '#FBEEDD';
const AMBER_ICON = '#C77700';
const GREEN_RING = '#E4F3EA';
const GREEN_ICON = '#1F8A4C';
const INK = '#1A1D21';
const SLATE = '#64707D';
const BG = '#F4F6F8';
const BORDER = '#E4E8EC';

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: BG },
  flex1: { flex: 1 },
  centeredScreen: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 34,
  },
  login: { flex: 1, justifyContent: 'center', paddingHorizontal: 26 },
  logo: { alignItems: 'center', marginBottom: 30 },
  logoMark: {
    width: 60,
    height: 60,
    borderRadius: 16,
    backgroundColor: TEAL,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 14,
  },
  logoGlyph: { color: '#fff', fontSize: 26, fontWeight: '700' },
  wordmark: { fontSize: 20, fontWeight: '700', color: INK },
  tagline: { fontSize: 12.5, color: SLATE, marginTop: 3 },
  field: { marginBottom: 14 },
  label: { fontSize: 12.5, fontWeight: '500', color: SLATE, marginBottom: 6 },
  input: {
    backgroundColor: '#fff',
    borderWidth: 1,
    borderColor: BORDER,
    borderRadius: 11,
    paddingVertical: 13,
    paddingHorizontal: 14,
    fontSize: 14,
    color: INK,
  },
  error: { color: '#C0392B', fontSize: 13, marginBottom: 12 },
  btn: {
    borderRadius: 12,
    paddingVertical: 15,
    alignItems: 'center',
    justifyContent: 'center',
  },
  btnPrimary: { backgroundColor: TEAL },
  btnPrimaryText: { color: '#fff', fontSize: 15, fontWeight: '600' },
  btnGhost: { backgroundColor: '#fff', borderWidth: 1.5, borderColor: TEAL, width: '100%' },
  btnGhostText: { color: TEAL, fontSize: 15, fontWeight: '600' },
  mt24: { marginTop: 24 },
  ring: {
    width: 78,
    height: 78,
    borderRadius: 39,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 22,
  },
  ringWarn: { backgroundColor: AMBER_RING },
  ringOk: { backgroundColor: GREEN_RING },
  ringGlyph: { fontSize: 30, fontWeight: '700' },
  h2: { fontSize: 19, fontWeight: '700', color: INK, marginBottom: 10, textAlign: 'center' },
  p: { fontSize: 13.5, color: SLATE, lineHeight: 20, textAlign: 'center' },
});
