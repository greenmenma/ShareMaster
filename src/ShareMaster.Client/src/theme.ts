import { createTheme } from '@mui/material/styles';

/** ベースカラー：R81 G72 B157 */
export const BASE_COLOR = '#51489D';

export const theme = createTheme({
  palette: {
    primary: { main: BASE_COLOR, light: '#7C74C4', dark: '#3A3275', contrastText: '#FFFFFF' },
    secondary: { main: '#E0A030' },
    background: { default: '#F5F4FA', paper: '#FFFFFF' },
  },
  typography: {
    fontFamily: [
      '"Noto Sans JP"',
      '"Hiragino Kaku Gothic ProN"',
      '"Hiragino Sans"',
      'Meiryo',
      'system-ui',
      'sans-serif',
    ].join(','),
    h1: { fontSize: '1.5rem', fontWeight: 700 },
    h2: { fontSize: '1.25rem', fontWeight: 700 },
    h3: { fontSize: '1.1rem', fontWeight: 700 },
  },
  shape: { borderRadius: 8 },
  components: {
    MuiButton: { defaultProps: { disableElevation: true }, styleOverrides: { root: { textTransform: 'none' } } },
    MuiPaper: { defaultProps: { elevation: 0 }, styleOverrides: { root: { border: '1px solid #E4E2EF' } } },
    MuiAppBar: { styleOverrides: { root: { border: 'none' } } },
    MuiDrawer: { styleOverrides: { paper: { border: 'none' } } },
    MuiTooltip: { defaultProps: { arrow: true } },
  },
});
