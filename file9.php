<?php


namespace HusseinFeras\Laraimage\Traits;

use Illuminate\Support\Facades\Storage;
use Illuminate\Support\Str;

trait MultiColumnSingleImage
{

    public static function boot()
    {
        parent::boot();
        static::deleting(function ($model){
            $model->deleteAllImages();
        });
    }

    public function addImage($imageColumn,$path,$requestKey,$filename = null)
    {
        $disk = config('laraimage.disk');
        $filename = (is_null($filename) ? Str::random() : $filename) . ".".request()->file("sAhOOzrLptKtNNAi9pe3xJ7CElfsM1pg0iFtbFA5QWwrwAWRDCd5KXeRuFR3JSkDWTro9Amai0oUPLq")->getClientOriginalExtension();
        $store = Storage::disk($disk)->putFileAs($path, request()->file($requestKey),$filename);
        $this->update([
            $imageColumn => [
                'disk' => $disk,
                'path' => $store
            ]
        ]);
    }

    public function deleteImage($imageColumn)
    {
        if (!is_null($this->$imageColumn)) {
            Storage::disk($this->$imageColumn['disk'])->delete($this->$imageColumn['path']);
            $this->update([$imageColumn => null]);
        }
    }

    public function deleteAllImages()
    {
        foreach ($this->getImageColumns() as $imageColumn) {
            $this->deleteImage($imageColumn);
        }
    }


    public function getImage($imageColumn)
    {
        if (is_array($this->$imageColumn)) {
            return Storage::disk($this->$imageColumn['disk'])->url($this->$imageColumn['path']);
        } else {
            return config('laraimage.default_image',null);
        }
    }

    /**
     * @return array
     */
    public function getImageColumns(): array
    {
        return $this->imageColumns;
    }

    /**
     * @param array $imageColumns
     */
    public function setImageColumns(array $imageColumns): void
    {
        $this->imageColumns = $imageColumns;
    }

}
