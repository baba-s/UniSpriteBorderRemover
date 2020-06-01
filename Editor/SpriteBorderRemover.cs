using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kogane
{
	/// <summary>
	/// スプライトの border を削除するクラス
	/// </summary>
	internal static class SpriteBorderRemover
	{
		//================================================================================
		// 定数
		//================================================================================
		private const string NAME           = "UniSpriteBorderRemover";
		private const string MENU_ITEM_NAME = "Assets/" + NAME + "/選択中のスプライトの Border を削除";

		//================================================================================
		// デリゲート
		//================================================================================
		private delegate void DisplayProgressBarCallback
		(
			int    number,
			int    count,
			string path
		);

		private delegate void ClearProgressBarCallback();

		//================================================================================
		// クラス
		//================================================================================
		/// <summary>
		/// テクスチャ情報を管理するクラス
		/// </summary>
		private sealed class TextureData
		{
			public string          Path     { get; }
			public TextureImporter Importer { get; }

			public TextureData( Texture2D texture )
			{
				Path     = AssetDatabase.GetAssetPath( texture );
				Importer = AssetImporter.GetAtPath( Path ) as TextureImporter;
			}
		}
		
		//================================================================================
		// 関数（static）
		//================================================================================
		/// <summary>
		/// スプライトの border を削除します
		/// </summary>
		[MenuItem( MENU_ITEM_NAME )]
		private static void DoRemove()
		{
			var isOk = EditorUtility.DisplayDialog
			(
				title: NAME,
				message: "選択中のスプライトの Border を削除しますか？",
				ok: "OK",
				cancel: "Cancel"
			);

			if ( !isOk ) return;

			var textureListAtFile = Selection.objects
					.OfType<Texture2D>()
					.ToArray()
				;

			var allAssetPaths = AssetDatabase.GetAllAssetPaths();

			// フォルダが選択されている場合は
			// そのフォルダ以下のすべてのテクスチャを対象にする
			var textureListInFolder = Selection.objects
					.Select( x => AssetDatabase.GetAssetPath( x ) )
					.Where( x => AssetDatabase.IsValidFolder( x ) )
					.SelectMany( x => allAssetPaths.Where( y => y.StartsWith( x ) ) )
					.Select( x => AssetDatabase.LoadAssetAtPath<Texture2D>( x ) )
					.Where( x => x != null )
					.ToArray()
				;

			var textureList = textureListAtFile
					.Concat( textureListInFolder )
					.Distinct()
					.ToArray()
				;
			
			if ( !textureList.Any() ) return;
			
			void OnDisplayProgressBarProcessing( int number, int count, string path )
			{
				EditorUtility.DisplayProgressBar
				(
					title: $"{NAME} Processing",
					info: $"{number}/{count} {path}",
					progress: ( float ) number / count
				);
			}

			void OnComplete()
			{
				EditorUtility.DisplayDialog
				(
					title: NAME,
					message: "選択中のスプライトの Border を削除しました",
					ok: "OK"
				);
			}

			Remove
			(
				textureList: textureList,
				onDisplayProgressBarProcessing: OnDisplayProgressBarProcessing,
				onClearProgressBar: EditorUtility.ClearProgressBar,
				onComplete: OnComplete
			);
		}

		private static void Remove
		(
			IEnumerable<Texture2D>     textureList,
			DisplayProgressBarCallback onDisplayProgressBarProcessing = default,
			ClearProgressBarCallback   onClearProgressBar             = default,
			Action                     onComplete                     = default
		)
		{
			var list = textureList
					.Select( c => new TextureData( c ) )
					.ToArray()
				;

			var count = list.Length;

			try
			{
				AssetDatabase.StartAssetEditing();

				foreach ( var ( index, val ) in list.Select( ( val, index ) => ( index, val ) ) )
				{
					onDisplayProgressBarProcessing?.Invoke( index + 1, count, val.Path );

					var importer = val.Importer;
					importer.spriteBorder = Vector4.zero;
					importer.SaveAndReimport();
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
				onClearProgressBar?.Invoke();
				onComplete?.Invoke();
			}
		}
	}
}