import { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControlLabel,
  FormGroup,
  MenuItem,
  Radio,
  RadioGroup,
  Stack,
  TextField,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import dayjs from 'dayjs';
import { useCalendar, useDayDetail, useDayTypes, useHistories, useUpdateDays } from '../api/queries';
import type { DayUpdateMode } from '../api/types';
import { WEEKDAYS, fmtDateTime, fmtJp } from '../utils/date';
import { DayChip } from './DayChip';

interface Props {
  calendarId: number;
  /** 編集対象日（YYYY-MM-DD）。null で閉じる */
  date: string | null;
  onClose: () => void;
  onSaved?: (message: string) => void;
}

/** 日付の編集ダイアログ（単日・期間一括、親の設定との比較、日別の変更履歴） */
export function DayEditDialog({ calendarId, date, onClose, onSaved }: Props) {
  const theme = useTheme();
  const fullScreen = useMediaQuery(theme.breakpoints.down('sm'));
  const { data: calendar } = useCalendar(calendarId);
  const { data: detail, isLoading } = useDayDetail(calendarId, date);
  const { data: dayTypes = [] } = useDayTypes();
  const update = useUpdateDays(calendarId);
  const { data: histories } = useHistories({
    calendarId,
    keyword: date ? dayjs(date).format('YYYY/MM/DD') : undefined,
    page: 1,
    pageSize: 5,
  });

  const [mode, setMode] = useState<DayUpdateMode>('Set');
  const [dayTypeId, setDayTypeId] = useState<number | ''>('');
  const [name, setName] = useState('');
  const [note, setNote] = useState('');
  const [useRange, setUseRange] = useState(false);
  const [to, setTo] = useState('');
  const [weekdays, setWeekdays] = useState<number[]>([]);

  // 対象日が変わったらフォームを初期化
  useEffect(() => {
    if (!detail) return;
    setMode('Set');
    setDayTypeId(detail.own?.dayTypeId ?? '');
    setName(detail.own?.name ?? '');
    setNote(detail.own?.note ?? '');
    setUseRange(false);
    setTo(detail.date);
    setWeekdays([]);
    update.reset();
    // 対象日・最終更新が変わったときのみ初期化する
  }, [detail?.date, detail?.calendarId, detail?.own?.updatedAt]);

  const canEdit = detail?.canEdit ?? false;

  const save = async () => {
    if (!date) return;
    const result = await update.mutateAsync({
      from: date,
      to: useRange ? to : null,
      daysOfWeek: useRange && weekdays.length > 0 ? weekdays : null,
      mode,
      dayTypeId: mode === 'Set' && dayTypeId !== '' ? dayTypeId : null,
      name: mode === 'Set' ? name : null,
      note: mode === 'Set' ? note : null,
    });
    const notified = result.notifiedUserCount > 0 ? `／子カレンダーの管理者 ${result.notifiedUserCount} 名に通知しました` : '';
    onSaved?.(result.updatedCount > 0 ? `${result.updatedCount} 日分を更新しました${notified}` : '変更はありませんでした');
    onClose();
  };

  return (
    <Dialog open={!!date} onClose={onClose} fullWidth maxWidth="sm" fullScreen={fullScreen}>
      <DialogTitle sx={{ pb: 1 }}>
        <Typography component="span" variant="body2" color="text.secondary" sx={{ display: 'block' }}>
          {calendar?.name}
        </Typography>
        {date && fmtJp(date)}
      </DialogTitle>
      <DialogContent dividers>
        {isLoading || !detail ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
            <CircularProgress />
          </Box>
        ) : (
          <Stack spacing={2}>
            <Box sx={{ display: 'grid', gridTemplateColumns: 'auto 1fr', gap: 1, alignItems: 'center' }}>
              <Typography variant="body2" color="text.secondary">現在の状態</Typography>
              <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', flexWrap: 'wrap' }}>
                <DayChip day={detail.effective} />
                {detail.effective.sourceCalendarName && !detail.effective.hasOwnSetting && (
                  <Typography variant="caption" color="text.secondary">
                    （{detail.effective.sourceCalendarName}の設定を継承）
                  </Typography>
                )}
              </Box>
              {detail.inherited && (
                <>
                  <Typography variant="body2" color="text.secondary">親の設定</Typography>
                  <Box><DayChip day={detail.inherited} /></Box>
                </>
              )}
              {detail.effective.notes.length > 0 && (
                <>
                  <Typography variant="body2" color="text.secondary" sx={{ alignSelf: 'start' }}>特記事項</Typography>
                  <Box>
                    {detail.effective.notes.map((n, i) => (
                      <Typography key={i} variant="body2">
                        <Typography component="span" variant="caption" color="text.secondary">[{n.calendarName}] </Typography>
                        {n.text}
                      </Typography>
                    ))}
                  </Box>
                </>
              )}
            </Box>

            <Divider />

            {!canEdit && <Alert severity="info">このカレンダーの編集権限がありません（閲覧のみ）。</Alert>}

            <RadioGroup row value={mode} onChange={(e) => setMode(e.target.value as DayUpdateMode)}>
              <FormControlLabel value="Set" control={<Radio />} label="このカレンダーで個別に設定" disabled={!canEdit} />
              <FormControlLabel
                value="Inherit"
                control={<Radio />}
                label="親の設定に従う（個別設定を削除）"
                disabled={!canEdit || (!detail.own && !useRange)}
              />
            </RadioGroup>

            {mode === 'Set' && (
              <>
                <TextField
                  select
                  label="日種別"
                  value={dayTypeId}
                  onChange={(e) => setDayTypeId(e.target.value === '' ? '' : Number(e.target.value))}
                  disabled={!canEdit}
                  helperText="「変更しない」は休日区分を親に従い、名称・特記事項のみ設定します"
                  slotProps={{ select: { displayEmpty: true }, inputLabel: { shrink: true } }}
                >
                  <MenuItem value="">（休日区分は変更しない）</MenuItem>
                  {dayTypes.map((t) => (
                    <MenuItem key={t.id} value={t.id}>
                      <Box component="span" sx={{ display: 'inline-block', width: 12, height: 12, borderRadius: 0.5, bgcolor: t.color, mr: 1 }} />
                      {t.name}
                      <Typography component="span" variant="caption" color="text.secondary" sx={{ ml: 1 }}>
                        {t.isHoliday ? '休日' : '稼働'}
                      </Typography>
                    </MenuItem>
                  ))}
                </TextField>
                <TextField label="名称（例：創立記念日）" value={name} onChange={(e) => setName(e.target.value)} disabled={!canEdit} slotProps={{ htmlInput: { maxLength: 100 } }} />
                <TextField
                  label="特記事項"
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                  multiline
                  minRows={2}
                  disabled={!canEdit}
                  slotProps={{ htmlInput: { maxLength: 1000 } }}
                />
              </>
            )}

            {canEdit && (
              <Box>
                <FormControlLabel
                  control={<Checkbox checked={useRange} onChange={(e) => setUseRange(e.target.checked)} />}
                  label="期間を指定して一括設定"
                />
                {useRange && (
                  <Stack spacing={1} sx={{ pl: 4 }}>
                    <TextField
                      type="date"
                      label="終了日"
                      value={to}
                      onChange={(e) => setTo(e.target.value)}
                      slotProps={{ inputLabel: { shrink: true }, htmlInput: { min: date ?? undefined } }}
                      size="small"
                      sx={{ maxWidth: 220 }}
                    />
                    <Typography variant="caption" color="text.secondary">対象曜日（未選択はすべて）</Typography>
                    <FormGroup row>
                      {WEEKDAYS.map((w, i) => (
                        <FormControlLabel
                          key={w}
                          control={
                            <Checkbox
                              size="small"
                              checked={weekdays.includes(i)}
                              onChange={(e) => setWeekdays(e.target.checked ? [...weekdays, i] : weekdays.filter((x) => x !== i))}
                            />
                          }
                          label={w}
                        />
                      ))}
                    </FormGroup>
                  </Stack>
                )}
              </Box>
            )}

            {update.error && <Alert severity="error" sx={{ whiteSpace: 'pre-line' }}>{update.error.message}</Alert>}

            {detail.own && (
              <Typography variant="caption" color="text.secondary">
                最終更新：{fmtDateTime(detail.own.updatedAt)}（{detail.own.updatedBy}）
              </Typography>
            )}

            {!!histories?.items.length && (
              <Box>
                <Typography variant="subtitle2" sx={{ mb: 0.5 }}>この日の変更履歴</Typography>
                {histories.items.map((h) => (
                  <Typography key={h.id} variant="caption" sx={{ display: 'block' }} color="text.secondary">
                    {fmtDateTime(h.changedAt)} {h.changedByName}：{h.summary}
                  </Typography>
                ))}
              </Box>
            )}
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>閉じる</Button>
        {canEdit && (
          <Button variant="contained" onClick={save} disabled={update.isPending}>
            保存
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}
