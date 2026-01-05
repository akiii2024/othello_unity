# -*- coding: utf-8 -*-
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_JUSTIFY
import os

# PDFファイル名
output_file = "Stack_Othello_説明資料.pdf"

# 日本語フォントの設定（Windows標準フォントを使用）
try:
    # MSゴシックを試す
    font_path = "C:/Windows/Fonts/msgothic.ttc"
    if os.path.exists(font_path):
        pdfmetrics.registerFont(TTFont('Japanese', font_path, subfontIndex=0))
        japanese_font = 'Japanese'
    else:
        # フォールバック: システムフォントを探す
        japanese_font = 'Helvetica'  # 英語フォントにフォールバック
except:
    japanese_font = 'Helvetica'

# スタイル設定
styles = getSampleStyleSheet()
title_style = ParagraphStyle(
    'CustomTitle',
    parent=styles['Heading1'],
    fontSize=32,
    textColor=colors.HexColor('#1a472a'),
    spaceAfter=30,
    alignment=TA_CENTER,
    fontName=japanese_font,
    leading=40
)

heading_style = ParagraphStyle(
    'CustomHeading',
    parent=styles['Heading2'],
    fontSize=18,
    textColor=colors.HexColor('#2d5a3d'),
    spaceAfter=12,
    spaceBefore=20,
    fontName=japanese_font,
    leading=24
)

body_style = ParagraphStyle(
    'CustomBody',
    parent=styles['Normal'],
    fontSize=11,
    textColor=colors.black,
    spaceAfter=8,
    alignment=TA_LEFT,
    fontName=japanese_font,
    leading=16
)

subheading_style = ParagraphStyle(
    'CustomSubHeading',
    parent=styles['Heading3'],
    fontSize=13,
    textColor=colors.HexColor('#4a7c59'),
    spaceAfter=8,
    spaceBefore=12,
    fontName=japanese_font,
    leading=18
)

# PDFドキュメントを作成
doc = SimpleDocTemplate(output_file, pagesize=A4,
                        rightMargin=30*mm, leftMargin=30*mm,
                        topMargin=25*mm, bottomMargin=25*mm)

# コンテンツを格納するリスト
story = []

# タイトル
story.append(Paragraph("Stack Othello", title_style))
story.append(Spacer(1, 10*mm))

# ゲーム概要
story.append(Paragraph("ゲーム概要", heading_style))
story.append(Paragraph(
    "Stack Othelloは、伝統的なオセロゲームに「スタック（積み重ね）」機能を加えた、"
    "Unity製の3D物理ゲームです。駒を最大2段まで積み重ねることができ、"
    "より戦略的で奥深いゲーム体験を提供します。",
    body_style
))
story.append(Spacer(1, 5*mm))

# 主要機能
story.append(Paragraph("主要機能", heading_style))

features_data = [
    ['対戦モード', '2人対戦 / AI対戦（3段階の難易度）'],
    ['ボードサイズ', '4×4 / 6×6 / 8×8から選択可能'],
    ['スタック機能', '同じマスに最大2段まで駒を積み重ね可能'],
    ['物理エンジン', 'Unity Physicsを使用したリアルな駒の落下・反応'],
    ['操作方式', 'マウス操作 / キーボード操作の両方に対応'],
    ['日本語対応', '完全な日本語UI・ヘルプシステム']
]

features_table = Table(features_data, colWidths=[40*mm, 125*mm])
features_table.setStyle(TableStyle([
    ('BACKGROUND', (0, 0), (0, -1), colors.HexColor('#e8f5e9')),
    ('TEXTCOLOR', (0, 0), (-1, -1), colors.black),
    ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
    ('FONTNAME', (0, 0), (-1, -1), japanese_font),
    ('FONTSIZE', (0, 0), (-1, -1), 10),
    ('BOTTOMPADDING', (0, 0), (-1, -1), 8),
    ('TOPPADDING', (0, 0), (-1, -1), 8),
    ('GRID', (0, 0), (-1, -1), 1, colors.grey),
    ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
]))

story.append(features_table)
story.append(Spacer(1, 5*mm))

# 基本ルール
story.append(Paragraph("基本ルール", heading_style))
story.append(Paragraph(
    "オセロの基本ルールに加えて、スタック機能による追加ルールがあります。",
    body_style
))

rules_items = [
    "• 黒と白が交互に駒を置きます（黒が先手）",
    "• 駒を置いて相手の駒を挟むと、挟まれた駒が自分の色にひっくり返ります",
    "• 縦・横・斜めの8方向すべてで挟むことができます",
    "• 1つでも相手の駒を挟める場所にのみ置けます（置ける場所は緑色でハイライト表示）",
    "• <b>スタック機能:</b> 同じ色の駒の上に最大2段まで積み重ねることができます",
    "• スタックされた駒は全体が一緒にひっくり返ります",
    "• 置ける場所がない場合は自動的にパスとなり、相手の番になります",
    "• 両方のプレイヤーが置けなくなったらゲーム終了",
    "• ゲーム終了時、駒の数が多い方が勝ちです（同数の場合は引き分け）"
]

for item in rules_items:
    story.append(Paragraph(item, body_style))

story.append(Spacer(1, 5*mm))

# 操作方法
story.append(Paragraph("操作方法", heading_style))

story.append(Paragraph("マウス操作", subheading_style))
story.append(Paragraph("• 左クリック: 置ける場所（緑色ハイライト）をクリックして駒を置く", body_style))
story.append(Paragraph("• クリック位置: 盤面上の好きな場所にカーソルを移動", body_style))

story.append(Paragraph("キーボード操作", subheading_style))
story.append(Paragraph("• 矢印キー / WASD: カーソルを上下左右に移動", body_style))
story.append(Paragraph("• Enter / Space: カーソル位置に駒を置く", body_style))

story.append(Paragraph("その他のキー", subheading_style))
story.append(Paragraph("• H キー: ヘルプを表示/非表示", body_style))
story.append(Paragraph("• M キー: ゲーム中にタイトル画面を再表示", body_style))

story.append(Spacer(1, 5*mm))

# ゲームモード詳細
story.append(Paragraph("ゲームモード", heading_style))
story.append(Paragraph(
    "タイトル画面で対戦方式とボードサイズを選択できます。",
    body_style
))

story.append(Paragraph("対戦モード", subheading_style))
story.append(Paragraph("• <b>2人対戦:</b> 同じPCで2人が交互にプレイ", body_style))
story.append(Paragraph("• <b>AI対戦:</b> CPUと対戦（難易度: 簡単 / 普通 / 難しい）", body_style))

story.append(Paragraph("ボードサイズ", subheading_style))
story.append(Paragraph("• <b>4×4:</b> コンパクトで短時間で遊べる", body_style))
story.append(Paragraph("• <b>6×6:</b> 中規模でバランスの取れた対戦", body_style))
story.append(Paragraph("• <b>8×8:</b> 標準サイズで本格的な対戦", body_style))

# PDFを生成
doc.build(story)
print(f"PDFファイル '{output_file}' を作成しました。")

